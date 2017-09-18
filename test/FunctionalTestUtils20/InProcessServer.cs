namespace FunctionalTestUtils
{
    using System;
    using System.IO;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;    

    // a variant of aspnet/Hosting/test/Microsoft.AspNetCore.Hosting.Tests/HostingEngineTests.cs
    public class InProcessServer : IDisposable
    {
        private static Random random = new Random();

        public static Func<IWebHostBuilder, IWebHostBuilder> UseApplicationInsights =
            builder => builder.UseApplicationInsights();

        private readonly Func<IWebHostBuilder, IWebHostBuilder> configureHost;
        private IWebHost hostingEngine;
        private string url;

        private TelemetryHttpListenerObservable listener;
        private readonly ServerTelemetryChannel channel;

        public ServerTelemetryChannel Channel
        {
            get
            {
                return this.channel;
            }
        }

        public TelemetryHttpListenerObservable Listener
        {
            get
            {
                return this.listener;
            }
        }

        public InProcessServer(string assemblyName, Func<IWebHostBuilder, IWebHostBuilder> configureHost = null)
        {
            this.configureHost = configureHost;
            var machineName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            this.url = "http://" + machineName + ":" + random.Next(5000, 14000).ToString();

            this.listener = new TelemetryHttpListenerObservable(url);
            this.listener.Start();

            this.channel = this.Start(assemblyName);
        }

        public string BaseHost
        {
            get
            {
                return this.url;
            }
        }

        public IServiceProvider ApplicationServices { get; private set; }

        private ServerTelemetryChannel Start(string assemblyName)
        {
            var builder = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls(this.BaseHost)
                .UseKestrel()
                .UseStartup(assemblyName)
                .UseEnvironment("Production");
            if (configureHost != null)
            {
                builder = configureHost(builder);
            }

            this.hostingEngine = builder.Build();

            this.hostingEngine.Start();

            this.ApplicationServices = this.hostingEngine.Services;
            return (ServerTelemetryChannel)this.hostingEngine.Services.GetService<ITelemetryChannel>();
        }

        public void Dispose()
        {
            if (this.listener != null)
            {
                this.listener.Stop();
            }

            if (this.hostingEngine != null)
            {
                this.hostingEngine.Dispose();
            }
        }
    }
}
