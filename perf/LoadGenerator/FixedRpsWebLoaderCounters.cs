using System;
using System.Collections.Generic;
using System.Text;

namespace LoadGenerator
{
    using System.Threading;

    public class FixedRpsWebLoaderCounters
    {
        private CounterValues values = new CounterValues();

        public void IncrementIterationsComplete()
        {
            Interlocked.Increment(ref values._iterationsComplete);
        }

        public void IncrementItemsQueued()
        {
            Interlocked.Increment(ref values._itemsQueued);
        }

        public void IncrementItemsProcessed()
        {
            Interlocked.Increment(ref values._itemsProcessed);
        }

        public void IncrementItemsProcessedWithErrors()
        {
            Interlocked.Increment(ref values._itemsProcessedWithErrors);
        }

        public CounterValues Reset()
        {
            var result = values;

            Interlocked.Exchange(ref values._iterationsComplete, 0);
            Interlocked.Exchange(ref values._itemsQueued, 0);
            Interlocked.Exchange(ref values._itemsProcessed, 0);
            Interlocked.Exchange(ref values._itemsProcessedWithErrors, 0);

            return result;
        }

        public struct CounterValues
        {
            public long _iterationsComplete;
            public long _itemsQueued;
            public long _itemsProcessed;
            public long _itemsProcessedWithErrors;

            public long IterationsComplete
            {
                get { return this._iterationsComplete; }
            }

            public long ItemsQueued
            {
                get { return this._itemsQueued; }
            }

            public long ItemsProcessed
            {
                get { return this._itemsProcessed; }
            }

            public long ItemsProcessedWithErrors
            {
                get { return this._itemsProcessedWithErrors; }
            }
        }
    }
}
