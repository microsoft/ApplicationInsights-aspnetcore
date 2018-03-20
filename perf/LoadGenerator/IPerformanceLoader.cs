using System;
using System.Collections.Generic;
using System.Text;

namespace LoadGenerator
{
    using System.Collections.Generic;

    /// <summary>
    /// 
    /// </summary>
    public interface IPerformanceLoader
    {
        void Initialze(IDictionary<string, object> settings);

        /// <summary>
        /// 
        /// </summary>
        void StartLoad();

        /// <summary>
        /// 
        /// </summary>
        void StopLoad();
    }
}
