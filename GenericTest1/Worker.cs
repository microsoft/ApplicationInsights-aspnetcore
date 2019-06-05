using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;

namespace GenericTest1
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private static HttpClient client = new HttpClient();
        private TelemetryClient tc;

        public Worker(ILogger<Worker> logger, TelemetryClient tc)
        {
            _logger = logger;
            this.tc = tc;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (this.tc.StartOperation<RequestTelemetry>("Worker Operation"))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    var res = client.GetStringAsync("http://google.com").Result;
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
