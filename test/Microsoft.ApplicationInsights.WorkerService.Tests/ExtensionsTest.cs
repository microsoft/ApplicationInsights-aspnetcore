using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.ApplicationInsights.WorkerService.Tests
{
    public class ExtensionsTests
    {
        private readonly ITestOutputHelper output;
        public const string TestInstrumentationKey = "11111111-2222-3333-4444-555555555555";
        public const string TestEndPoint = "http://testendpoint/v2/track";
        public ExtensionsTests(ITestOutputHelper output)
        {
            this.output = output;
            this.output.WriteLine("Initialized");
        }

        private static ServiceCollection CreateServicesAndAddApplicationinsightsWorker(Action<ApplicationInsightsServiceOptions> serviceOptions = null)
        {
            var services = new ServiceCollection();
            services.AddApplicationInsightsTelemetryWorkerService();
            if (serviceOptions != null)
            {
                services.Configure(serviceOptions);
            }
            return services;
        }

        [Theory]
        [InlineData(typeof(ITelemetryInitializer), typeof(ApplicationInsights.AspNetCore.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryInitializer), typeof(AzureWebAppRoleEnvironmentTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryInitializer), typeof(ComponentVersionTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryInitializer), typeof(HttpDependenciesParsingTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(TelemetryConfiguration), null, ServiceLifetime.Singleton)]
        [InlineData(typeof(TelemetryClient), typeof(TelemetryClient), ServiceLifetime.Singleton)]
        public void RegistersExpectedServices(Type serviceType, Type implementationType, ServiceLifetime lifecycle)
        {
            var services = CreateServicesAndAddApplicationinsightsWorker(null);
            ServiceDescriptor service = services.Single(s => s.ServiceType == serviceType && s.ImplementationType == implementationType);
            Assert.Equal(lifecycle, service.Lifetime);
        }

        [Theory]
        [InlineData(typeof(ITelemetryInitializer), typeof(ApplicationInsights.AspNetCore.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryInitializer), typeof(AzureWebAppRoleEnvironmentTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryInitializer), typeof(ComponentVersionTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryInitializer), typeof(HttpDependenciesParsingTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(TelemetryConfiguration), null, ServiceLifetime.Singleton)]
        [InlineData(typeof(TelemetryClient), typeof(TelemetryClient), ServiceLifetime.Singleton)]
        public void RegistersExpectedServicesOnlyOnce(Type serviceType, Type implementationType, ServiceLifetime lifecycle)
        {
            var services = CreateServicesAndAddApplicationinsightsWorker(null);
            services.AddApplicationInsightsTelemetryWorkerService();
            ServiceDescriptor service = services.Single(s => s.ServiceType == serviceType && s.ImplementationType == implementationType);
            Assert.Equal(lifecycle, service.Lifetime);
        }

        [Fact]
        public void DoesNotThrowWithoutInstrumentationKey()
        {
            var services = CreateServicesAndAddApplicationinsightsWorker(null);
        }

        [Fact]
        public void ReadsSettingsFromSuppliedConfiguration()
        {
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "sample-appsettings.json");

            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).Build();
            var services = new ServiceCollection();
            
            services.AddApplicationInsightsTelemetryWorkerService(config);

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
            Assert.Equal(TestEndPoint, telemetryConfiguration.TelemetryChannel.EndpointAddress);
            Assert.Equal(true, telemetryConfiguration.TelemetryChannel.DeveloperMode);
        }
    }
}
