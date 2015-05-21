namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Http;
    using Microsoft.AspNet.Hosting;
    using Microsoft.ApplicationInsights.AspNet.Tracing;

    public class OperationIdTelemetryInitializer : TelemetryInitializerBase
    {
        public OperationIdTelemetryInitializer(IHttpContextAccessor httpContextAccessor, AspNet5EventSource eventSource)
             : base(httpContextAccessor, eventSource)
        { }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Operation.Id))
            {
                telemetry.Context.Operation.Id = requestTelemetry.Id;
            }
        }
    }
}