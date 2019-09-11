namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;
    using Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// A telemetry initializer that populates cloud context role instance.
    /// </summary>
    public class DomainNameRoleInstanceTelemetryInitializer : ITelemetryInitializer
    {
        private string roleInstanceName;

        /// <summary>
        /// Initializes role instance name and node name with the host name.
        /// </summary>
        /// <param name="telemetry">Telemetry item.</param>
        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleInstance))
            {
                var name = LazyInitializer.EnsureInitialized(ref this.roleInstanceName, this.GetMachineName);
                telemetry.Context.Cloud.RoleInstance = name;
            }
        }

        private string GetMachineName()
        {
            string hostName = Dns.GetHostName();

            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            if (!hostName.EndsWith(domainName, StringComparison.OrdinalIgnoreCase))
            {
                hostName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", hostName, domainName);
            }

            return hostName;
        }
    }
}