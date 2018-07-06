namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using Microsoft.ApplicationInsights.Extensibility;
    using Xunit;

    public class RequestTrackingTelemetryModuleTest
    {
        [Fact]
        public void RequestTrackingTelemetryModuleDoesNoThrowWhenAppIdProviderisNull()
        {
            RequestTrackingTelemetryModule requestTrackingTelemetryModule = new RequestTrackingTelemetryModule(null);  
        }

        [Fact]
        public void RequestTrackingTelemetryModuleDoesNoThrowIfInitializeAfterDispose()
        {
            RequestTrackingTelemetryModule requestTrackingTelemetryModule = new RequestTrackingTelemetryModule(null);            
            requestTrackingTelemetryModule.Dispose();
            requestTrackingTelemetryModule.Initialize(new TelemetryConfiguration());
        }
    }
}
