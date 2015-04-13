namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    public class OperationIdTelemetryInitializerTest
    {
        [Fact]
        public void ConstructorThrowsIfRequestTelemetryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var initializer = new OperationIdTelemetryInitializer(null); });
        }

        [Fact]
        public void InitializeDoesNotOverrideOperationIdProvidedInline()
        {
            var initializer = new OperationIdTelemetryInitializer(new RequestTelemetry());

            var telemetry = new EventTelemetry();
            telemetry.Context.Operation.Id = "123";
            initializer.Initialize(telemetry);

            Assert.Equal("123", telemetry.Context.Operation.Id);
        }

        [Fact]
        public void InitializeSetsTelemetryOperationIdToRequestId()
        {
            var request = new RequestTelemetry { Id = "123" };
            var initializer = new OperationIdTelemetryInitializer(request);

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal(request.Id, telemetry.Context.Operation.Id);
        }
    }
}