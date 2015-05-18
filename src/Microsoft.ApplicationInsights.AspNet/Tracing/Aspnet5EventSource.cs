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
        public void VerboseTelemetryInitializerNotEnabledOnHttpContextNull()
        {
            this.WriteEvent(1);
        }

        [Event(2, Message = "Telemetry Initializer will not collect data when RequestServices are not available.", Level = EventLevel.Verbose)]
        public void VerboseTelemetryInitializerNotEnabledOnRequestServicesNull()
        {
            this.WriteEvent(2);
        }

        [NonEvent]
        public void ErrorTelemetryInitializerFailedToCollectData(Exception exception)
        {
            this.ErrorTelemetryInitializerFailedToCollectData(exception.ToString());
        }

        [Event(3, Message = "Telemetry Initializer failed to collect data. Exception: {0}", Level = EventLevel.Error)]
        public void ErrorTelemetryInitializerFailedToCollectData(string exception)
        {
            this.WriteEvent(3, exception);
        }

        [Event(4, Message = "TelemetryContext is null in context initializer.", Level = EventLevel.Verbose)]
        public void VerboseTelemetryContextNotAvailableInContextInitializer()
        {
            this.WriteEvent(4);
        }

        [Event(5, Message ="Malformed cookie {0}: value {1}. This may indicate misconfiguiration of JavaScript SDK.", Level = EventLevel.Error)]
        public void ErrorMalformedCookie(string cookieName, string cookieValue)
        {
            this.WriteEvent(5);
        }

        [Event(10, Message = "Application Insights services should be registered. Use method AddApplicationInsightsTelemetry in application startup.", 
            Level = EventLevel.Error, Keywords = Keywords.UserActionable)]
        public void UserActionableErrorRegisterApplicationInsightsServices()
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
