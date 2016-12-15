using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.AspNetCore.Internal
{
    public class HostingEventSourceListener : EventListener
    {
        private readonly TelemetryClient _telemetryClient;

        public HostingEventSourceListener(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName != "EventCounters")
            {
                return;
            }

            if (eventData.Payload.Count > 0)
            {
                var payload = eventData.Payload[0] as IDictionary<string, object>;
                if (payload != null)
                {
                    var eventCounterPayload = new EventCounterPayload(payload);
                    _telemetryClient.TrackMetric(new MetricTelemetry()
                    {
                        Name = eventCounterPayload.Name,

                        // Number of samples
                        Count = eventCounterPayload.Count,

                        // EventCounter api does not have a 'total' or 'sum' equivalent property.
                        Value = eventCounterPayload.Mean * eventCounterPayload.Count,

                        StandardDeviation = eventCounterPayload.StandardDeviation,
                        Max = eventCounterPayload.Max,
                        Min = eventCounterPayload.Min,
                        Timestamp = DateTimeOffset.Now,
                    });
                }
            }
        }
    }
}