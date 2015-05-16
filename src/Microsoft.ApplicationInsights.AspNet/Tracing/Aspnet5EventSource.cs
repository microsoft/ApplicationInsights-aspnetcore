namespace Microsoft.ApplicationInsights.AspNet.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading.Tasks;

    internal class Aspnet5EventSource : EventSource
    {
        /// <summary>
        /// Instance of the Aspnet5EventSource class.
        /// </summary>
        public static readonly Aspnet5EventSource Log = new Aspnet5EventSource();

        private Aspnet5EventSource()
        {
        }

        [Event(1, Message = "TelemetryInitializerNotEnabledOnHttpContextNull", Level = EventLevel.Verbose)]
        public void TelemetryInitializerNotEnabledOnHttpContextNull()
        {
            this.WriteEvent(1);
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
