namespace SampleWebAppIntegration.FunctionalTest
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    public class DependencyTelemetryMvcTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework45.FunctionalTests";

        [Fact]
        public void OperationIdOfRequestIsPropagatedToChildDependency()
        {
            // https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/340
            // Verify operation of OperationIdTelemetryInitializer
            InProcessServer server;
            using (server = new InProcessServer(assemblyName, InProcessServer.UseApplicationInsights))
            {
                using (var httpClient = new HttpClient())
                {
                    var task = httpClient.GetAsync(server.BaseHost + "/Home/Dependency");
                    task.Wait(TestTimeoutMs);
                }
            }

            var telemetries = server.BackChannel.Buffer;
            try
            {
                Assert.Equal(2, telemetries.Count);
                Assert.Equal(telemetries[0].Context.Operation.Id, telemetries[1].Context.Operation.Id);
            }
            catch (Exception e)
            {
                string data = DebugTelemetryItems(telemetries);
                throw new Exception(data, e);
            }
        }

        [Fact]
        public void ParentIdOfChildDependencyIsIdOfRequest()
        {
            // https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/333
            // Verify operation of OperationCorrelationTelemetryInitializer
            InProcessServer server;
            using (server = new InProcessServer(assemblyName, InProcessServer.UseApplicationInsights))
            {
                using (var httpClient = new HttpClient())
                {
                    var task = httpClient.GetAsync(server.BaseHost + "/Home/Dependency");
                    task.Wait(TestTimeoutMs);
                }
            }

            var telemetries = server.BackChannel.Buffer;
            try
            {
                Assert.Equal(2, telemetries.Count);
                Assert.IsType(typeof(DependencyTelemetry), telemetries[0]);
                Assert.IsType(typeof(RequestTelemetry), telemetries[1]);
                Assert.Equal(((RequestTelemetry)telemetries[1]).Id, telemetries[0].Context.Operation.ParentId);
            }
            catch (Exception e)
            {
                string data = DebugTelemetryItems(telemetries);
                throw new Exception(data, e);
            }
        }

        private string DebugTelemetryItems(IList<ITelemetry> telemetries)
        {
            StringBuilder builder = new StringBuilder();
            foreach (ITelemetry telemetry in telemetries)
            {
                DependencyTelemetry dependency = telemetry as DependencyTelemetry;
                if (dependency != null) {
                    builder.AppendLine($"{dependency.ToString()} - {dependency.Data} - {dependency.Duration} - {dependency.Id} - {dependency.Name} - {dependency.ResultCode} - {dependency.Sequence} - {dependency.Success} - {dependency.Target} - {dependency.Type}");
                } else {
                    builder.AppendLine($"{telemetry.ToString()} - {telemetry.Context?.Operation?.Name}");
                }
            }

            return builder.ToString();
        }
    }
}
