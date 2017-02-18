using System;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.AspNetCore.Internal
{
    public struct EventCounterPayload
    {
        public EventCounterPayload(IDictionary<string, object> payload)
        {
            Payload = payload;
        }

        private IDictionary<string, object> Payload { get; }

        public string Name
        {
            get
            {
                return Payload[nameof(Name)].ToString();
            }
        }

        public double Mean
        {
            get
            {
                return Convert.ToDouble(Payload[nameof(Mean)]);
            }
        }

        public double StandardDeviation
        {
            get
            {
                return Convert.ToDouble(Payload["StandardDerivation"]); // typo in EventCounter api which was fixed later
            }
        }

        public int Count
        {
            get
            {
                return Convert.ToInt32(Payload[nameof(Count)]);
            }
        }

        public double Min
        {
            get
            {
                return Convert.ToDouble(Payload[nameof(Min)]);
            }
        }

        public double Max
        {
            get
            {
                return Convert.ToDouble(Payload[nameof(Max)]);
            }
        }

        public double IntervalSec
        {
            get
            {
                return Convert.ToDouble(Payload[nameof(IntervalSec)]);
            }
        }
    }
}
