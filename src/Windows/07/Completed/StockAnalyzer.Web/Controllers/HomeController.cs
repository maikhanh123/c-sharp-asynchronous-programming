using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StockAnalyzer.Core.Domain;
using StockAnalyzer.Web.Models;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace StockAnalyzer.Web.Controllers;

public class HomeController : Controller
{
    private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private Stopwatch stopwatch = new Stopwatch();
    private Random random = new Random();

    public async Task<IActionResult> Index()
    {
        using (var client = new HttpClient())
        {
            //var responseTask = client.GetAsync($"{API_URL}/MSFT");

            //var response = await responseTask;

            //var content = await response.Content.ReadAsStringAsync();

            //var data = JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);

            var stocks = new Dictionary<string, IEnumerable<StockPrice>>
            {
                { "MSFT1", Generate("MSFT") },
                { "GOOGL", Generate("GOOGL") },
                { "AAPL", Generate("AAPL") },
                { "CAT", Generate("CAT") },
                { "ABC", Generate("ABC") },
                { "DEF", Generate("DEF") },
                { "DEF1", Generate("DEF1") },
                { "DEF2", Generate("DEF2") }
            };

            var bag = new ConcurrentBag<StockCalculation>();

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Parallel.For(0, 10, (i, state) => {
                            // i == current index
                        });

                        var parallelLoopResult = Parallel.ForEach(stocks,
                            new ParallelOptions { MaxDegreeOfParallelism = 7 },
                            (element, state) => {
                                if (element.Key == "MSFT" || state.ShouldExitCurrentIteration)
                                {
                                    state.Break();

                                    return;
                                }
                                else
                                {
                                    var result = Calculate(element.Value);
                                    bag.Add(result);
                                }
                            });
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                
            }
            var data = bag;

            return View(data);
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private StockCalculation Calculate(IEnumerable<StockPrice> prices)
    {
        #region Start stopwatch
        var calculation = new StockCalculation();
        var watch = new Stopwatch();
        watch.Start();
        #endregion

        var end = DateTime.UtcNow.AddSeconds(4);

        // Spin a loop for a few seconds to simulate load
        while (DateTime.UtcNow < end)
        { }

        #region Return a result
        calculation.Identifier = prices.First().Identifier;
        calculation.Result = prices.Average(s => s.Open);

        watch.Stop();

        calculation.TotalSeconds = watch.Elapsed.Seconds;

        return calculation;
        #endregion
    }

    private IEnumerable<StockPrice> Generate(string stockIdentifier)
    {
        return Enumerable.Range(1, random.Next(10, 250))
            .Select(x => new StockPrice
            {
                Identifier = stockIdentifier,
                Open = random.Next(10, 1024)
            });
    }

}