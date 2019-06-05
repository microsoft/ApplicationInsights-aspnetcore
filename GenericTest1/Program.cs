using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace GenericTest1
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var host = new HostBuilder();
            host.ConfigureServices(services =>
                {
                     services.AddHostedService<Worker>();
                     services.AddApplicationInsightsTelemetryNoHttp("3a4ade35-6d7b-41bc-8c00-03aaa3495ab9");
                });

            host.ConfigureLogging((hostContext, config) =>
            {
                config.AddDebug();
                config.AddConsole();
            });

            return host;
        }
    }
}
