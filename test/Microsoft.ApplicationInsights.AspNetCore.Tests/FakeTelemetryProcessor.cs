namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    internal class FakeTelemetryProcessor : ITelemetryProcessor, ITelemetryModule
    {
        private readonly ITelemetryProcessor next;

        public FakeTelemetryProcessor(ITelemetryProcessor next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            this.next = next;
            this.Initialized = false;
        }

        public bool Initialized { get; private set; }

        public void Initialize(TelemetryConfiguration configuration)
        {
            this.Initialized = true;
        }

        public void Process(ITelemetry item)
        {
            this.next.Process(item);
        }
    }
}
