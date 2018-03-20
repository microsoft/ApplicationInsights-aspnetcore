using System;
using System.Collections.Generic;
using System.Text;

namespace LoadGenerator
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// 
    /// </summary>
    public class FixedRpsWebLoadersSettings
    {
        private const string TragetUriKey = "TargetUri";
        private const string MaxRequestsPerSecondKey = "MaxRequestsPerSecond";
        private const string HeadersKey = "Headers";

        private readonly IDictionary<string, object> settings;

        private readonly Uri targetUri;
        private readonly int maxRequestsPerSecondKey;
        private readonly IDictionary headers;

        public FixedRpsWebLoadersSettings(
            IDictionary<string, object> settings)
        {
            if (null == settings)
            {
                throw new ArgumentNullException("settings");
            }

            this.settings = settings;

            this.targetUri = new Uri(InternalReadValue(TragetUriKey).ToString());
            this.maxRequestsPerSecondKey = Convert.ToInt32(InternalReadValue(MaxRequestsPerSecondKey));
            this.headers = InternalReadValue(HeadersKey, isOptional: true) as IDictionary;
        }

        public IDictionary Headers
        {
            get { return this.headers; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Uri TargetUri
        {
            get { return this.targetUri; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int MaxRequestsPerSecond
        {
            get { return this.maxRequestsPerSecondKey; }
        }

        private object InternalReadValue(string key, bool isOptional = false)
        {
            object value = null;
            if (false == this.settings.TryGetValue(key, out value))
            {
                if (!isOptional)
                {
                    throw new KeyNotFoundException(
                        string.Format("Unable to find setting, name:{0}", key));
                }
            }

            return value;
        }
    }
}
