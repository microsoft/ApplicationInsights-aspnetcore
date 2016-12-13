namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using Extensions;
    using Internal;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.ApplicationInsights.AspNetCore.Logging;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Class used to initialize Application Insight diagnostic listeners.
    /// </summary>
    internal class ApplicationInsightsInitializer : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly List<IDisposable> subscriptions;
        private readonly IEnumerable<IApplicationInsightDiagnosticListener> diagnosticListeners;
        private readonly HostingEventSourceListener hostingEventSourceListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsInitializer"/> class.
        /// </summary>
        public ApplicationInsightsInitializer(
            IOptions<ApplicationInsightsServiceOptions> options,
            IEnumerable<IApplicationInsightDiagnosticListener> diagnosticListeners,
            ILoggerFactory loggerFactory,
            DebugLoggerControl debugLoggerControl,
            TelemetryClient client,
            HostingEventSourceListener hostingEventSourceListener)
        {
            this.diagnosticListeners = diagnosticListeners;
            this.subscriptions = new List<IDisposable>();
            this.hostingEventSourceListener = hostingEventSourceListener;

            // Add default logger factory for debug mode only if enabled and instrumentation key not set
            if (options.Value.EnableDebugLogger && string.IsNullOrEmpty(options.Value.InstrumentationKey))
            {
                // Do not use extension method here or it will disable debug logger we currently adding
                loggerFactory.AddProvider(new ApplicationInsightsLoggerProvider(client, (s, level) => debugLoggerControl.EnableDebugLogger && Debugger.IsAttached));
            }
        }

        /// <summary>
        /// Subscribes diagnostic listeners to sources
        /// </summary>
        public void Start()
        {
            DiagnosticListener.AllListeners.Subscribe(this);

            // TODO: Ideally we do not want to start this listener until we want to
            var hostingEventSource = EventSource.GetSources()
                .Where(eventSource => eventSource.Name == "Microsoft-AspNetCore-Hosting")
                .FirstOrDefault();
            if (hostingEventSource != null)
            {
                var args = new Dictionary<string, string>();
                args.Add("EventCounterIntervalSec", "1");

                // Sample EventCounter info
                //< Event MSec = "4247.7123" PID = "20476" PName = "dotnet" TID = "23864" EventName = "EventCounters"
                //  TimeStamp = "12/14/16 14:35:17.603350" ID = "27" Version = "0" Keywords = "0x00000000" TimeStampQPC = "2,376,979,690,831"
                //  Level = "Always" ProviderName = "Microsoft-AspNetCore-Hosting" ProviderGuid = "9e620d2a-55d4-5ade-deb7-c26046d245a8" ClassicProvider = "False"
                //  Opcode = "0" Task = "Default" Channel = "11" PointerSize = "8"
                //  CPU = "2" EventIndex = "145728" TemplateType = "DynamicTraceEventData" >
                //      < PrettyPrint >
                //        < Event MSec = "4247.7123" PID = "20476" PName = "dotnet" TID = "23864" EventName = "EventCounters" ProviderName = "Microsoft-AspNetCore-Hosting" Payload = "{ Name=&quot;Request&quot;, Mean=NaN, StandardDerivation=NaN, Count=0, Min=0, Max=0, IntervalSec=1.000162 }" />
                //               </ PrettyPrint >
                //               < Payload Length = "40" >
                //                     0:   e  0 52  0 65  0 71  0 | 75  0 65  0 73  0 74  0..R.e.q.u.e.s.t.
                //      10:   0  0 c0 ff  0  0 c0 ff | 0  0  0  0  0  0  0  0........ ........
                //      20:   0  0  0  0 4f  5 80 3f |                           ....O..?
                //  </ Payload >
                //</ Event >

                //TODO: figure out when to disable listening for these events (Dispose does not seem right as this initializer instance is singleton
                // which means it would remain as long as the app is up)
                this.hostingEventSourceListener.EnableEvents(hostingEventSource, EventLevel.LogAlways, EventKeywords.None, args);
            }
        }

        /// <inheritdoc />
        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener value)
        {
            foreach (var applicationInsightDiagnosticListener in this.diagnosticListeners)
            {
                if (applicationInsightDiagnosticListener.ListenerName == value.Name)
                {
                    this.subscriptions.Add(value.SubscribeWithAdapter(applicationInsightDiagnosticListener));
                }
            }
        }

        /// <inheritdoc />
        void IObserver<DiagnosticListener>.OnError(Exception error)
        {
        }

        /// <inheritdoc />
        void IObserver<DiagnosticListener>.OnCompleted()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var subscription in this.subscriptions)
                {
                    subscription.Dispose();
                }
            }
        }
    }
}