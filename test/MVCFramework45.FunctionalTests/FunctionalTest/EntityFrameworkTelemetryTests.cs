namespace MVCFramework45.FunctionalTests.FunctionalTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    public class EntityFrameworkTelemetryTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework45.FunctionalTests";

        [Fact]
        public void TestEntityFrameworkTelemetryItemsReceived()
        {
            InProcessServer server;
            using (server = new InProcessServer(assemblyName, InProcessServer.UseApplicationInsights))
            {
                using (var httpClient = new HttpClient())
                {
                    var task = httpClient.GetAsync(server.BaseHost + "/Home/Contact");
                    task.Wait(TestTimeoutMs);
                }
            }
            var telemetries = server.BackChannel.Buffer.OfType<DependencyTelemetry>()
                .Where(t => t.Type == "SQL" && t.Target == "aspnet-MVCFramework45.FunctionalTests-60cfc765-2dc9-454c-bb34-dc379ed92cd0")
                .ToArray();

            Assert.True(telemetries.Length >= 2);
            Assert.All(telemetries, telemetry =>
            {
                Assert.StartsWith("SELECT ", telemetry.Data);
                Assert.Equal(telemetry.Target, telemetry.Name);
            });
        }
    }
}
