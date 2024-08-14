using Newtonsoft.Json;
using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using StockAnalyzer.Core.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    // Testing control StateMachine for async await
    //private async void Search_Click(object sender, RoutedEventArgs e)
    //{
    //    var result = await new StateMachineDemo().Run();
    //    Notes.Text = result;
    //}

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        try
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

            cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Token.Register(() =>
            {
                Notes.Text = "Cancellation requested";
            });

            Search.Content = "Cancel"; // Button text

            BeforeLoadingStockData();

            var identifiers = StockIdentifier.Text.Split(' ', ',');

            var data = new ObservableCollection<StockPrice>();

            Stocks.ItemsSource = data;

            //var service = new MockStockStreamService();
            var service = new StockDiskStreamService();
            var enumerator = service.GetAllStockPrices(cancellationTokenSource.Token);

            await foreach (var item in enumerator)
            {
                if (identifiers.Contains(item.Identifier))
                {
                    await Task.Delay(500, cancellationTokenSource.Token);
                    data.Add(item);
                }
            }



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

    private async Task<IEnumerable<StockPrice>>
        GetStocksFor(string identifier)
    {
        var service = new StockService();
        var data = await service.GetStockPricesFor(identifier,
            CancellationToken.None).ConfigureAwait(false);



        return data.Take(5);
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