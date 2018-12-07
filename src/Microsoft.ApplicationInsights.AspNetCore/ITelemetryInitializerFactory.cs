namespace Microsoft.ApplicationInsights.AspNetCore
{
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Factory for creating instance of the <see cref="ITelemetryInitializer"/>.
    /// </summary>
    public interface ITelemetryInitializerFactory
    {
        /// <summary>
        /// Creates an instance of the <see cref="ITelemetryInitializer"/> to be used.
        /// </summary>
        ITelemetryInitializer Create();
    }
}