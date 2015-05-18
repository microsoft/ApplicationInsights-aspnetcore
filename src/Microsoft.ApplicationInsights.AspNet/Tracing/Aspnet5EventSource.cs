namespace Microsoft.ApplicationInsights.AspNet.Tracing
{
    using System.Diagnostics.Tracing;

    [EventSource(Name = "Microsoft-ApplicationInsights-AspNet5")]
    public class AspNet5EventSource : EventSource
    {
        public AspNet5EventSource()
        {
        }

        [Event(1, Message = "Telemetry Initializer is not enabled when HttpContext is null", Level = EventLevel.Verbose)]
        public void TelemetryInitializerNotEnabledOnHttpContextNull()
        {
            this.WriteEvent(1);
        }

        protected virtual void WriteEventEx(int eventId)
        {
            this.WriteEvent(eventId);
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
