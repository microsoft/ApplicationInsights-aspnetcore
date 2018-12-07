namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.DependencyInjection;

    internal class TelemetryInitializerFactory : ITelemetryInitializerFactory
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Type telemetryInitializerType;

        /// <summary>
        /// Constructs an instance of the factory.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="telemetryInitializerType">The type of <see cref="ITelemetryInitializer"/> to create.</param>
        public TelemetryInitializerFactory(IServiceProvider serviceProvider, Type telemetryInitializerType)
        {
            this.serviceProvider = serviceProvider;
            this.telemetryInitializerType = telemetryInitializerType;
        }

        /// <summary>
        /// Creates an instance of the <see cref="ITelemetryInitializer"/> to be used.
        /// </summary>
        public ITelemetryInitializer Create()
        {
            return (ITelemetryInitializer)ActivatorUtilities.CreateInstance(this.serviceProvider, this.telemetryInitializerType);
        }
    }
}
