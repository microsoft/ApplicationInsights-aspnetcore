namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    public class OperationIdTelemetryInitializer : ITelemetryInitializer
    {
        private readonly RequestTelemetry request;

        public OperationIdTelemetryInitializer(RequestTelemetry request)
        {
            this.request = request;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Operation.Id))
            {
                telemetry.Context.Operation.Id = request.Id;
            }
        }
    }
}