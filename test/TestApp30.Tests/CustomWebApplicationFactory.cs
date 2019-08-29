using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestApp30.Tests
{
    public class CustomWebApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup> where TStartup : class
    {
        internal IList<ITelemetry> sentItems = new List<ITelemetry>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ITelemetryChannel>(new StubChannel() 
                {
                    OnSend = (item) => this.sentItems.Add(item)
                });
                services.AddApplicationInsightsTelemetry("somekey");

                // Build the service provider.
                var sp = services.BuildServiceProvider();
                var tc = sp.GetRequiredService<TelemetryClient>();
            });
        }
    }
}
