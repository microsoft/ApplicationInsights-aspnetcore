using System;
using System.Collections.Generic;
using System.Threading;

namespace LoadGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            IDictionary<string, object> defaultSettings = new Dictionary<string, object>
            {
                {
                    "TargetUri",
                    //new Uri("http://localhost:5000/api/values")
                    new Uri(args[0])
                },
                {
                    "MaxRequestsPerSecond",
                    args[1]
                   // 50
                },
                {
                    "Headers",
                    new Dictionary<string, string>
                    {
                        {
                            "user-agent",
                            "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)"
                        }
                    }
                }
            };
            FixedRpsWebLoader loader = new FixedRpsWebLoader();
            loader.Initialze(defaultSettings);
            loader.StartLoad();
            Thread.Sleep(int.Parse(args[2]));
            //Thread.Sleep(30000);
            FixedRpsWebLoaderCounters.CounterValues values = loader.Counters.Reset();
            loader.StopLoad();
            Console.WriteLine(values.ItemsProcessed);            
        }
    }
}
