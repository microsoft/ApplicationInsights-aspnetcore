namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Hosting;

    /// <summary>
    /// <see cref="ITelemetryInitializer"/> implementation that stamps ASP.NET Core environment name
    /// on telemetries.
    /// </summary>
    internal class AspNetCoreEnvironmentTelemetryInitializer: ITelemetryInitializer
    {
        private const string AspNetCoreEnvironmentPropertyName = "AspNetCoreEnvironment";
        private readonly IHostingEnvironment _environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetCoreEnvironmentTelemetryInitializer"/> class.
        /// </summary>
        public AspNetCoreEnvironmentTelemetryInitializer(IHostingEnvironment environment)
        {
            _environment = environment;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (_environment != null && !telemetry.Context.Properties.ContainsKey(AspNetCoreEnvironmentPropertyName))
            {
                telemetry.Context.Properties.Add(AspNetCoreEnvironmentPropertyName, _environment.EnvironmentName);
            }
        }
    }
}
