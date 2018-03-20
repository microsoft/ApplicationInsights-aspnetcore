using System;
using System.Collections.Generic;
using System.Text;

namespace LoadGenerator
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;

    /// <summary>
    /// 
    /// </summary>
    public class FixedRpsWebLoader : PerformanceLoaderBase
    {
        private FixedRpsWebLoadersSettings set;
        private readonly ManualResetEvent operateEvent = new ManualResetEvent(false);
        private Timer schedulingTimer;

        private const int DefaultMinimalWindowsTimerResultionInMs = 15;
        private const int MaxWorkItemsPerWorkerPerSecond = 50;
        private const int MaxWorkers = 40;

        private readonly List<WorkerConfig> workerPool = new List<WorkerConfig>();
        private readonly ConcurrentBag<WorkItem> workItems = new ConcurrentBag<WorkItem>();

        private int workItemsPerInterval = 0;
        private int schedulerInterval = 0;
        private int workItemsPerWorkerPerInterval = 0;

        private readonly FixedRpsWebLoaderCounters counters = new FixedRpsWebLoaderCounters();

        public FixedRpsWebLoaderCounters Counters
        {
            get { return this.counters; }
        }

        protected override void OnInitialize(
            IDictionary<string, object> settings)
        {
            this.set = new FixedRpsWebLoadersSettings(settings);

            var numberOfWorkers = this.set.MaxRequestsPerSecond / MaxWorkItemsPerWorkerPerSecond;
            if ((this.set.MaxRequestsPerSecond % MaxWorkItemsPerWorkerPerSecond) != 0)
            {
                ++numberOfWorkers;
            }

            // multiply by 3 because we found out that for Avg app that has long response time we do not drain the queue of 
            // workitems fast enough (and we want to do requests synchronously)
            this.workItemsPerWorkerPerInterval = 3 * this.set.MaxRequestsPerSecond / numberOfWorkers;
            if (0 == this.workItemsPerWorkerPerInterval)
            {
                this.workItemsPerWorkerPerInterval = 1;
            }

            if (numberOfWorkers > MaxWorkers)
            {
                numberOfWorkers = MaxWorkers;
            }

            this.schedulerInterval = 1000 / this.set.MaxRequestsPerSecond;
            if (this.schedulerInterval < DefaultMinimalWindowsTimerResultionInMs)
            {
                this.schedulerInterval = DefaultMinimalWindowsTimerResultionInMs;
            }

            this.workItemsPerInterval = this.set.MaxRequestsPerSecond / (1000 / this.schedulerInterval);
            if (this.workItemsPerInterval == 0)
            {
                this.workItemsPerInterval = 1;
            }

            for (int idx = 0; idx < numberOfWorkers; ++idx)
            {
                workerPool.Add(
                    new WorkerConfig(
                        this.operateEvent,
                        this.workItems,
                        this.workItemsPerWorkerPerInterval,
                        this.counters));
            }

            ServicePointManager.DefaultConnectionLimit = numberOfWorkers;
        }

        protected override void OnStartLoad()
        {
            for (int idx = 0; idx < this.workerPool.Count; ++idx)
            {
                var th = new Thread(WorkerConfig.LoadThreadRoutine)
                {
                    Name = string.Format("Load Thread {0}", idx)
                };

                th.Start(this.workerPool[idx]);

                this.workerPool[idx].Thread = th;
            }

            this.schedulingTimer = new Timer(this.SchedulerCallback);

            this.schedulingTimer.Change(
                TimeSpan.FromMilliseconds(0),
                TimeSpan.FromMilliseconds(schedulerInterval));
        }

        protected override void OnStopLoad()
        {
            this.schedulingTimer.Change(0, 0);

            const int ThreadAbortTimeout = 5000;
            foreach (var loadThreadConfig in this.workerPool)
            {
                loadThreadConfig.Canceled = true;
            }

            // waking up all the workers
            this.operateEvent.Set();

            foreach (var workerConfig in this.workerPool)
            {
                if (false == workerConfig.Thread.Join(ThreadAbortTimeout))
                {
                    try
                    {
                        //workerConfig.Thread.Abort();
                        //workerConfig.Thread
                    }
                    catch (ThreadAbortException)
                    {
                    }
                }
            }
        }

        private void SchedulerCallback(object stateInfo)
        {
            for (int idx = 0; idx < this.workItemsPerInterval; ++idx)
            {
                this.workItems.Add(new WorkItem(this.set.TargetUri, this.set.Headers));
                this.counters.IncrementItemsQueued();
            }

            // Notifying threads to start processing items
            this.operateEvent.Set();
            this.operateEvent.Reset();

            this.Counters.IncrementIterationsComplete();
        }

        private struct WorkItem
        {
            private readonly Uri targetUri;
            private readonly IDictionary headers;

            public WorkItem(Uri targetUri, IDictionary headers)
            {
                if (null == targetUri)
                {
                    throw new ArgumentNullException("targetUri");
                }

                this.targetUri = targetUri;
                this.headers = headers;
            }

            public void Execute()
            {
                if (null == this.targetUri)
                {
                    throw new InvalidOperationException("Work items is not initialized, please call constructor first");
                }

                var request = (HttpWebRequest)WebRequest.Create(this.targetUri);

                AddHeader(request, this.headers);

                // request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                using (var response = request.GetResponse())
                {
                    var buffer = new byte[10 * 1024];
                    using (var stream = response.GetResponseStream())
                    {
                        if (null != stream)
                        {
                            while (0 != stream.Read(buffer, 0, buffer.Length))
                            {
                            }
                        }
                    }
                }
            }

            private static void AddHeader(HttpWebRequest request, IDictionary headers)
            {
                if (headers != null && headers.Count > 0)
                {
                    foreach (object key in headers.Keys)
                    {
                        string keyStr = key.ToString().ToLowerInvariant();
                        string value = headers[key].ToString();

                        if (!WebHeaderCollection.IsRestricted(keyStr))
                        {
                            if (request.Headers.Get(keyStr) != null)
                            {
                                request.Headers.Set(keyStr, value);
                            }
                            else
                            {
                                request.Headers.Add(keyStr, value);
                            }
                        }
                        else
                        {
                            switch (keyStr)
                            {
                                case "accept":
                                    request.Accept = value;
                                    break;
                                case "connection":
                                    request.Connection = value;
                                    break;
                                case "content-length":
                                    request.ContentLength = long.Parse(value);
                                    break;
                                case "content-type":
                                    request.ContentType = value;
                                    break;
                                case "date":
                                    request.Date = DateTime.Parse(value);
                                    break;
                                case "expect":
                                    request.Expect = value;
                                    break;
                                case "host":
                                    request.Host = value;
                                    break;
                                case "if-modified-since":
                                    request.IfModifiedSince = DateTime.Parse(value);
                                    break;
                                case "referer":
                                    request.Referer = value;
                                    break;
                                case "transfer-encoding":
                                    request.TransferEncoding = value;
                                    break;
                                case "user-agent":
                                    request.UserAgent = value;
                                    break;
                                default: throw new NotSupportedException("Unsupported header: " + keyStr);
                            }
                        }
                    }
                }
            }
        }

        private class WorkerConfig
        {
            private readonly ManualResetEvent operateEvent;
            private readonly ConcurrentBag<WorkItem> workloadQueue;
            private readonly int itemsPerWorkerPerInterval;
            private readonly FixedRpsWebLoaderCounters fixedRpsWebLoaderCounters;

            private Thread thread;

            public WorkerConfig(
                ManualResetEvent operateEvent,
                ConcurrentBag<WorkItem> workloadQueue,
                int itemsPerWorkerPerInterval,
                FixedRpsWebLoaderCounters fixedRpsWebLoaderCounters)
            {
                this.operateEvent = operateEvent;
                this.workloadQueue = workloadQueue;
                this.itemsPerWorkerPerInterval = itemsPerWorkerPerInterval;
                this.fixedRpsWebLoaderCounters = fixedRpsWebLoaderCounters;
            }

            public Thread Thread
            {
                get { return this.thread; }
                set { this.thread = value; }
            }

            public bool Canceled { get; set; }

            public static void LoadThreadRoutine(object p)
            {
                var config = (WorkerConfig)p;

                while (false == config.Canceled)
                {
                    if (true == config.operateEvent.WaitOne())
                    {
                        WorkItem wi;
                        for (int idx = 0;
                            idx < config.itemsPerWorkerPerInterval
                            && true == config.workloadQueue.TryTake(out wi)
                            && false == config.Canceled;
                            ++idx)
                        {
                            try
                            {
                                wi.Execute();

                                config.fixedRpsWebLoaderCounters.IncrementItemsProcessed();
                            }
                            catch (Exception exp)
                            {
                                Trace.TraceInformation("Item processed with error:" + exp.Message);
                                config.fixedRpsWebLoaderCounters.IncrementItemsProcessedWithErrors();
                            }
                        }
                    }
                }
            }
        }
    }
}
