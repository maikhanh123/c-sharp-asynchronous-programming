using StockAnalyzer.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StockAnalyzer.Core.Services
{
    public class MockStockService : IStockService
    {
        public Task<IEnumerable<StockPrice>> GetStockPricesFor(string stockIdentifier, CancellationToken cancellationToken)
        {
            var stocks = new List<StockPrice>
                            {
                                new()
                                {
                                    Identifier = "MSFT",
                                    Change = 0.5m,
                                    ChangePercent = 0.75m
                                },
                                new()
                                {
                                    Identifier = "MSFT",
                                    Change = 0.5m,
                                    ChangePercent = 0.75m
                                },
                                new()
                                {
                                    Identifier = "GOOGL",
                                    Change = 0.5m,
                                    ChangePercent = 0.75m
                                },
                                new()
                                {
                                    Identifier = "GOOGL",
                                    Change = 0.5m,
                                    ChangePercent = 0.75m
                                }
                            };

            var task = Task.FromResult(stocks.Where(stock => stock.Identifier == stockIdentifier));

            return task;

        }
    }
}
