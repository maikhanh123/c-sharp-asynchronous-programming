using Newtonsoft.Json;
using StockAnalyzer.Core.Domain;
using System;
using System.Collections;
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

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        BeforeLoadingStockData();

        //var data = await GetStocksByHttpClient(StockIdentifier.Text);

        //if (!String.IsNullOrEmpty(data.ErrorMessage))
        //{
        //    Notes.Text = data.ErrorMessage;
        //}
        //else
        //{
        //    Stocks.ItemsSource = data.StocksList;
        //}

        await Task.Run(() =>
        {
            var lines = File.ReadAllLines("StockPrices_Small.csv");
            var data = new List<StockPrice>();
            foreach (var line in lines.Skip(1))
            {
                var price = StockPrice.FromCSV(line);
                data.Add(price);
            }

            Dispatcher.Invoke(() =>
            {
                Stocks.ItemsSource = data.Where(sp => sp.Identifier == StockIdentifier.Text);
            });
            
        });


        AfterLoadingStockData();
    }

    private void BeforeLoadingStockData()
    {
        stopwatch.Restart();
        StockProgress.Visibility = Visibility.Visible;
        StockProgress.IsIndeterminate = true;

        Stocks.ItemsSource = null;
        Notes.Text = String.Empty;
    }

    private void AfterLoadingStockData()
    {
        StocksStatus.Text = $"Loaded stocks for {StockIdentifier.Text} in {stopwatch.ElapsedMilliseconds}ms";
        StockProgress.Visibility = Visibility.Hidden;
    }

    private IEnumerable<StockPrice> GetStocksBywebClient()
    {
        var client = new WebClient();

        var content = client.DownloadString($"{API_URL}/{StockIdentifier.Text}");

        // Simulate that the web call takes a very long time
        Thread.Sleep(10000);

        var data = JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);
        return data;
    }

    private async Task<Result> GetStocksByHttpClient(string text)
    {
        // Simulate that the web call takes a very long time
        //await Task.Delay(5000);

        Result result = new Result();

        try
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{API_URL}/{text}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (String.IsNullOrEmpty(content))
                    {
                        throw new Exception("Deserialization resulted in null data.");
                    }
                    var data = JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);
                    if (data == null)
                    {
                        result.StocksList = new List<StockPrice>();
                        result.ErrorMessage = "Deserialization resulted in null data.";
                    }
                    else
                    {
                        result.StocksList = data;
                    }
                }
                else
                {
                    result.StocksList = new List<StockPrice>();
                    result.ErrorMessage = "Deserialization resulted in null data.";
                }
            }
        }
        catch (Exception ex)
        {
            result.StocksList = new List<StockPrice>();
            result.ErrorMessage = $"An error occurred while fetching stock prices. {ex}";
        }

        return result;
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

    class Result
    {
        public IEnumerable<StockPrice>? StocksList { get; set; }
        public string? ErrorMessage { get; set; }
    }
}