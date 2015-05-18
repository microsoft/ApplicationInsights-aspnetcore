namespace Microsoft.ApplicationInsights.AspNet.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit.Abstractions;

    public class Aspnet5EventTestListener : EventListener
    {
        private readonly List<EventWrittenEventArgs> events = new List<EventWrittenEventArgs>();

        private static Func<EventWrittenEventArgs, bool> issueDefinition = (message) => {
                return message.Level == EventLevel.Error
                    || message.Level == EventLevel.Critical
                    || message.Level == EventLevel.Warning;
            };

        private ITestOutputHelper output;

        public Aspnet5EventTestListener(ITestOutputHelper output)
        {
            this.output = output;
        }

        public IEnumerable<EventWrittenEventArgs> Events
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
            output.WriteLine("Trace: " + eventData.Message);
            this.events.Add(eventData);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.StartsWith("Microsoft-ApplicationInsights-", StringComparison.Ordinal))
            {
                this.EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)(-1L));
            }

            base.OnEventSourceCreated(eventSource);
        }

    }
}
