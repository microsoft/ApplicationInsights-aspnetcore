namespace Microsoft.ApplicationInsights.AspNet.Tests.Helpers
{
    using Microsoft.ApplicationInsights.AspNet.Tracing;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit.Abstractions;

    public class AspNet5EventTestListener : EventListener
    {
        public class TraceMessage
        {
            public string Message { get; set; }
            public EventLevel Level { get; set;  }
        }

        private readonly List<TraceMessage> events = new List<TraceMessage>();

        private static Func<TraceMessage, bool> issueDefinition = (message) => {
                return message.Level == EventLevel.Error
                    || message.Level == EventLevel.Critical
                    || message.Level == EventLevel.Warning;
            };

        public AspNet5EventTestListener(AspNet5EventSource eventSource)
        {
            this.EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)(-1));
        }

        public IList<TraceMessage> Events
        {
            get
            {
                return this.events;
            }
        }

        public bool HasIssues
        {
            get
            {
                return this.Events.Where(issueDefinition).Count() > 0;
            }
        }

        public string Issues
        {
            get
            {
                string issues = string.Empty;

                Parallel.ForEach(this.Events.Where(issueDefinition), (message) => { issues += message.Message + Environment.NewLine; });

                return issues;
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            this.events.Add(new TraceMessage()
            {
                Message = eventData.Message,
                Level = eventData.Level
            });
        }
    }
}
