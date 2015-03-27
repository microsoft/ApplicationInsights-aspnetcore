namespace Microsoft.ApplicationInsights.AspNet.Tests
{
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Mvc.Rendering;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.Framework.DependencyInjection;
    using Microsoft.Framework.DependencyInjection.Fallback;
    using Xunit;
    using System.IO;
    using Moq;

    public class ApplicationInsightsExtensionsTests
    {
        [Fact]
        public void AddTelemetryWillNotThrowWithoutInstrumentationKey()
        {
            try
            {
                var serviceCollection = HostingServices.Create(null);

                //Empty configuration that doesn't have instrumentation key
                IConfiguration config = new Configuration();

                serviceCollection.AddApplicationInsightsTelemetry(config);
            }
            finally
            {
                CleanActiveConfiguration();
            }
        }

        [Fact]
        public void AddTelemetryWillNotUseInstrumentationKeyFromConfig()
        {
            try
            {
                var serviceCollection = HostingServices.Create(null);
                IConfiguration config = new Configuration()
                    .AddJsonFile("content\\config.json");

                serviceCollection.AddApplicationInsightsTelemetry(config);

                Assert.Equal("11111111-2222-3333-4444-555555555555", TelemetryConfiguration.Active.InstrumentationKey);
            }
            finally
            {
                CleanActiveConfiguration();
            }
        }

        [Fact]
        public void AddTelemetryWillCreateTelemetryClientSingleton()
        {
            try
            {
                var serviceCollection = HostingServices.Create(null);
                IConfiguration config = new Configuration()
                    .AddJsonFile("content\\config.json");

                serviceCollection.AddApplicationInsightsTelemetry(config);

                Assert.Equal("11111111-2222-3333-4444-555555555555", TelemetryConfiguration.Active.InstrumentationKey);
                var serviceProvider = serviceCollection.BuildServiceProvider();

                var telemetryClient = serviceProvider.GetService<TelemetryClient>();
                Assert.NotNull(telemetryClient);
                Assert.Equal("11111111-2222-3333-4444-555555555555", telemetryClient.Context.InstrumentationKey);

            }
            finally
            {
                CleanActiveConfiguration();
            }
        }

        [Fact]
        public void JSSnippetWillNotThrowWithoutInstrumentationKey()
        {
            var helper = new Mock<IHtmlHelper>();
            helper.Object.ApplicationInsightsJavaScriptSnippet(null);
            helper.Object.ApplicationInsightsJavaScriptSnippet("");
        }

        [Fact]
        public void JSSnippetUsesInstrumentationKey()
        {
            var key = "1236543";
            var helper = new Mock<IHtmlHelper>();
            var result = helper.Object.ApplicationInsightsJavaScriptSnippet(key);
            using (StringWriter sw = new StringWriter())
            {
                result.WriteTo(sw);
                Assert.Contains(key, sw.ToString());
            }
        }

        private void CleanActiveConfiguration()
        {
            TelemetryConfiguration.Active.InstrumentationKey = "";
            TelemetryConfiguration.Active.ContextInitializers.Clear();
            TelemetryConfiguration.Active.TelemetryModules.Clear();
            TelemetryConfiguration.Active.TelemetryInitializers.Clear();
        }


    }
}