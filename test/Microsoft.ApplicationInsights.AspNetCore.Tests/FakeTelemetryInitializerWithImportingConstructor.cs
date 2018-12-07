using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    internal class FakeTelemetryInitializerWithImportingConstructor : ITelemetryInitializer
    {

        public FakeTelemetryInitializerWithImportingConstructor(IHostingEnvironment hostingEnvironment)
        {
            this.HostingEnvironment = hostingEnvironment;
            this.IsInitialized = true;
        }

        public IHostingEnvironment HostingEnvironment { get; }

        public bool IsInitialized { get; }

        public void Initialize(ITelemetry telemetry)
        {
        }
    }
}
