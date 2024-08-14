using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockAnalyzer.Windows
{
    public class StateMachineDemo
    {
        public Task<string> Run()
        {
            return Compute();
        }

        public Task<string> Compute()
        {
            return Load();
        }

        public Task<string> Load()
        {
            return Task.Run(() => "Pluralsight");
        }
    }

}
