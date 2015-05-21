namespace Microsoft.ApplicationInsights.AspNet.Tracing
{
    using System;
    using System.Diagnostics.Tracing;

    [EventSource(Name = "Microsoft-ApplicationInsights-AspNet5")]
    public class AspNet5EventSource : EventSource
    {
        public AspNet5EventSource()
        {
        }

        [Event(1, Message = "Telemetry Initializer will not collect data when HttpContext is null.", Level = EventLevel.Verbose)]
        public void TelemetryInitializerNotEnabledOnHttpContextNull()
        {
            this.WriteEvent(1);
        }

        [Event(2, Message = "Telemetry Initializer will not collect data when RequestServices are not available.", Level = EventLevel.Verbose)]
        public void TelemetryInitializerNotEnabledOnRequestServicesNull()
        {
            this.WriteEvent(2);
        }

        [NonEvent]
        public void TelemetryInitializerFailedToCollectData(Exception exception)
        {
            this.TelemetryInitializerFailedToCollectData(exception.ToString());
        }

        [Event(3, Message = "Telemetry Initializer failed to collect data. Exception: {0}", Level = EventLevel.Error)]
        public void TelemetryInitializerFailedToCollectData(string exception)
        {
            this.WriteEvent(3, exception);
        }

        [Event(4, Message = "TelemetryContext is null in context initializer.", Level = EventLevel.Verbose)]
        public void TelemetryContextNotAvailableInContextInitializer()
        {
            this.WriteEvent(4);
        }

        [Event(5, Message ="Malformed cookie {0}: value {1}. This may indicate misconfiguiration of JavaScript SDK.", Level = EventLevel.Error)]
        public void MalformedCookie(string cookieName, string cookieValue)
        {
            this.WriteEvent(5);
        }

        [Event(10, Message = "Application Insights services should be registered. Use method AddApplicationInsightsTelemetry in application startup.", 
            Level = EventLevel.Error, Keywords = Keywords.UserActionable)]
        public void RegisterApplicationInsightsServices()
        {
            this.WriteEvent(10);
        }

        public sealed class Keywords
        {
            /// <summary>
            /// Key word for user actionable events.
            /// </summary>
            public const EventKeywords UserActionable = (EventKeywords)0x1;            
        }
    }
}
