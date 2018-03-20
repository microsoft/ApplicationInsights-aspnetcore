using System;
using System.Collections.Generic;
using System.Text;

namespace LoadGenerator
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 
    /// </summary>
    public abstract class PerformanceLoaderBase : IPerformanceLoader
    {
        private IDictionary<string, object> set;
        private bool isInProgress = false;

        /// <summary>
        /// 
        /// </summary>
        protected IDictionary<string, object> Settings
        {
            get { return this.set; }
        }

        public void Initialze(IDictionary<string, object> settings)
        {
            if (null == settings)
            {
                throw new ArgumentNullException("settings");
            }

            this.EnsureNotInitialized();

            this.set = settings;

            this.OnInitialize(settings);
        }

        public void StartLoad()
        {
            this.EnsureInitialized();
            this.EnsureNotStarted();

            try
            {
                this.isInProgress = true;

                this.OnStartLoad();
            }
            catch (Exception)
            {
                this.isInProgress = false;

                throw;
            }
        }


        public void StopLoad()
        {
            this.EnsureInitialized();
            this.EnsureStarted();

            try
            {
                this.OnStopLoad();
            }
            catch (Exception)
            {
                this.isInProgress = false;

                throw;
            }
        }


        protected virtual void OnInitialize(
            IDictionary<string, object> settings)
        {
        }

        protected virtual void OnStartLoad()
        {
        }

        protected virtual void OnStopLoad()
        {
        }

        private void EnsureInitialized()
        {
            if (this.set == null)
            {
                throw new InvalidOperationException("Loader instance is not initialized");
            }
        }

        private void EnsureNotInitialized()
        {
            if (this.set != null)
            {
                throw new InvalidOperationException("Loader instance already initialized");
            }
        }

        private void EnsureStarted()
        {
            if (true != this.isInProgress)
            {
                throw new InvalidOperationException("Loader instance is not started");
            }
        }

        private void EnsureNotStarted()
        {
            if (true == this.isInProgress)
            {
                throw new InvalidOperationException("Loader instance already started");
            }
        }
    }
}
