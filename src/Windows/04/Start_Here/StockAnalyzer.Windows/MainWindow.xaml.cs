﻿using Newtonsoft.Json;
using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using StockAnalyzer.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace StockAnalyzer.Windows;

public partial class MainWindow : Window
{
    private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private Stopwatch stopwatch = new Stopwatch();

    public MainWindow()
    {
        InitializeComponent();
    }


    CancellationTokenSource? cancellationTokenSource;

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        if (cancellationTokenSource is not null)
        {
            // Already have an instance of the cancellation token source?
            // This means the button has already been pressed!

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            Search.Content = "Search";
            return;
        }

        try
        {
            cancellationTokenSource = new();

            cancellationTokenSource.Token.Register(() =>
            {
                Notes.Text = "Cancellation requested";
            });

            Search.Content = "Cancel"; // Button text

            BeforeLoadingStockData();

            var identifiers = StockIdentifier.Text.Split(',', ' ');

            var service = new StockService();

            var loadingTasks = new List<Task<IEnumerable<StockPrice>>>();

            foreach (var ident in identifiers)
            {
                var loadTask = service.GetStockPricesFor(ident, cancellationTokenSource.Token);

                loadingTasks.Add(loadTask);
            }

            // Test 1: get result no need to set time out
            //var allStocksLoadingTasks = await Task.WhenAll(loadingTasks);
            //Stocks.ItemsSource = allStocksLoadingTasks.SelectMany(x => x);


            // Test 2: get result with set time out
            var allStocksLoadingTasks = Task.WhenAll(loadingTasks);
            var timeout = Task.Delay(5000);
            
            var completedTask = await Task.WhenAny(allStocksLoadingTasks, timeout);

            if(completedTask == timeout)
            {
                cancellationTokenSource?.Cancel();
                throw new OperationCanceledException("Time out!");
            }

            Stocks.ItemsSource = allStocksLoadingTasks.Result.SelectMany(x => x);
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
        finally
        {
            AfterLoadingStockData();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            Search.Content = "Search";
        }
    }

    private static Task<List<string>> SearchForStocks(
        CancellationToken cancellationToken
    )
    {
        return Task.Run(async () =>
        {
            using var stream = new StreamReader(File.OpenRead("StockPrices_Small.csv"));

            var lines = new List<string>();

            while (await stream.ReadLineAsync() is string line)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                lines.Add(line);
            }

            return lines;
        }, cancellationToken);
    }

    private async Task GetStocks()
    {
        try
        {
            var store = new DataStore();

            var responseTask = store.GetStockPrices(StockIdentifier.Text);

            Stocks.ItemsSource = await responseTask;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private void BeforeLoadingStockData()
    {
        stopwatch.Restart();
        StockProgress.Visibility = Visibility.Visible;
        StockProgress.IsIndeterminate = true;
        Notes.Text = string.Empty;
        StocksStatus.Text = string.Empty;
        Stocks.ItemsSource = null;
    }

    private void AfterLoadingStockData()
    {
        StocksStatus.Text = $"Loaded stocks for {StockIdentifier.Text} in {stopwatch.ElapsedMilliseconds}ms";
        StockProgress.Visibility = Visibility.Hidden;
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });

        e.Handled = true;
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}