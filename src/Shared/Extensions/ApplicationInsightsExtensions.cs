namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    using Microsoft.ApplicationInsights;
#if AI_ASPNETCORE_WEB
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
#else
    using Microsoft.ApplicationInsights.WorkerService;
#endif
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;
#if NETSTANDARD2_0
    using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
#endif
    using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.WindowsServer;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> that allow adding Application Insights services to application.
    /// </summary>
    public static partial class ApplicationInsightsExtensions
    {
        private const string VersionKeyFromConfig = "version";
        private const string InstrumentationKeyFromConfig = "ApplicationInsights:InstrumentationKey";
        private const string DeveloperModeFromConfig = "ApplicationInsights:TelemetryChannel:DeveloperMode";
        private const string EndpointAddressFromConfig = "ApplicationInsights:TelemetryChannel:EndpointAddress";

        private const string InstrumentationKeyForWebSites = "APPINSIGHTS_INSTRUMENTATIONKEY";
        private const string DeveloperModeForWebSites = "APPINSIGHTS_DEVELOPER_MODE";
        private const string EndpointAddressForWebSites = "APPINSIGHTS_ENDPOINTADDRESS";


        /// <summary>
        /// Adds an Application Insights Telemetry Processor into a service collection via a <see cref="ITelemetryProcessorFactory"/>.
        /// </summary>
        /// <typeparam name="T">Type of the telemetry processor to add.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetryProcessor<T>(this IServiceCollection services)
            where T : ITelemetryProcessor
        {
            return services.AddSingleton<ITelemetryProcessorFactory>(serviceProvider =>
                new TelemetryProcessorFactory(serviceProvider, typeof(T)));
        }

        /// <summary>
        /// Adds an Application Insights Telemetry Processor into a service collection via a <see cref="ITelemetryProcessorFactory"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="telemetryProcessorType">Type of the telemetry processor to add.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">The <paramref name="telemetryProcessorType"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="telemetryProcessorType"/> type does not implement <see cref="ITelemetryProcessor"/>.</exception>
        public static IServiceCollection AddApplicationInsightsTelemetryProcessor(this IServiceCollection services,
            Type telemetryProcessorType)
        {
            if (telemetryProcessorType == null)
            {
                throw new ArgumentNullException(nameof(telemetryProcessorType));
            }

            if (!telemetryProcessorType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(ITelemetryProcessor)))
            {
                throw new ArgumentException(nameof(telemetryProcessorType) + "does not implement ITelemetryProcessor.");
            }

            return services.AddSingleton<ITelemetryProcessorFactory>(serviceProvider =>
                new TelemetryProcessorFactory(serviceProvider, telemetryProcessorType));
        }

        /// <summary>
        /// Extension method to provide configuration logic for application insights telemetry module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configModule">Action used to configure the module.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        [Obsolete("Use ConfigureTelemetryModule overload that accepts ApplicationInsightsServiceOptions.")]
        public static IServiceCollection ConfigureTelemetryModule<T>(this IServiceCollection services, Action<T> configModule)
            where T : ITelemetryModule
        {
            if (configModule == null)
            {
                throw new ArgumentNullException(nameof(configModule));
            }

            return services.AddSingleton(typeof(ITelemetryModuleConfigurator),
                new TelemetryModuleConfigurator((config, options) => configModule((T)config), typeof(T)));
        }

        /// <summary>
        /// Extension method to provide configuration logic for application insights telemetry module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configModule">Action used to configure the module.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection ConfigureTelemetryModule<T>(
            this IServiceCollection services,
            Action<T, ApplicationInsightsServiceOptions> configModule)
            where T : ITelemetryModule
        {
            if (configModule == null)
            {
                throw new ArgumentNullException(nameof(configModule));
            }

            return services.AddSingleton(typeof(ITelemetryModuleConfigurator),
                new TelemetryModuleConfigurator((config, options) => configModule((T)config, options), typeof(T)));
        }

        /// <summary>
        /// Adds Application Insight specific configuration properties to <see cref="IConfigurationBuilder"/>.
        /// </summary>
        /// <param name="configurationSourceRoot">The <see cref="IConfigurationBuilder"/> instance.</param>
        /// <param name="developerMode">Enables or disables developer mode.</param>
        /// <param name="endpointAddress">Sets telemetry endpoint address.</param>
        /// <param name="instrumentationKey">Sets instrumentation key.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddApplicationInsightsSettings(
            this IConfigurationBuilder configurationSourceRoot,
            bool? developerMode = null,
            string endpointAddress = null,
            string instrumentationKey = null)
        {
            var telemetryConfigValues = new List<KeyValuePair<string, string>>();

            bool wasAnythingSet = false;

            if (developerMode != null)
            {
                telemetryConfigValues.Add(new KeyValuePair<string, string>(DeveloperModeForWebSites,
#if !NETSTANDARD1_6
                    developerMode.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
#else
                    developerMode.Value.ToString()));
#endif
                wasAnythingSet = true;
            }

            if (instrumentationKey != null)
            {
                telemetryConfigValues.Add(new KeyValuePair<string, string>(InstrumentationKeyForWebSites,
                    instrumentationKey));
                wasAnythingSet = true;
            }

            if (endpointAddress != null)
            {
                telemetryConfigValues.Add(new KeyValuePair<string, string>(
                    EndpointAddressForWebSites,
                    endpointAddress));
                wasAnythingSet = true;
            }

            if (wasAnythingSet)
            {
                configurationSourceRoot.Add(new MemoryConfigurationSource() { InitialData = telemetryConfigValues });
            }

            return configurationSourceRoot;
        }

        /// <summary>
        /// Read from configuration
        /// Config.json will look like this:
        /// <para>
        ///      "ApplicationInsights": {
        ///          "InstrumentationKey": "11111111-2222-3333-4444-555555555555"
        ///          "TelemetryChannel": {
        ///              "EndpointAddress": "http://dc.services.visualstudio.com/v2/track",
        ///              "DeveloperMode": true
        ///          }
        ///      }.
        /// </para>
        /// Values can also be read from environment variables to support azure web sites configuration.
        /// </summary>
        /// <param name="config">Configuration to read variables from.</param>
        /// <param name="serviceOptions">Telemetry configuration to populate.</param>
        internal static void AddTelemetryConfiguration(IConfiguration config,
            ApplicationInsightsServiceOptions serviceOptions)
        {
            string instrumentationKey = config[InstrumentationKeyForWebSites];
            if (string.IsNullOrWhiteSpace(instrumentationKey))
            {
                instrumentationKey = config[InstrumentationKeyFromConfig];
            }

            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                serviceOptions.InstrumentationKey = instrumentationKey;
            }

            string developerModeValue = config[DeveloperModeForWebSites];
            if (string.IsNullOrWhiteSpace(developerModeValue))
            {
                developerModeValue = config[DeveloperModeFromConfig];
            }

            if (!string.IsNullOrWhiteSpace(developerModeValue))
            {
                bool developerMode = false;
                if (bool.TryParse(developerModeValue, out developerMode))
                {
                    serviceOptions.DeveloperMode = developerMode;
                }
            }

            string endpointAddress = config[EndpointAddressForWebSites];
            if (string.IsNullOrWhiteSpace(endpointAddress))
            {
                endpointAddress = config[EndpointAddressFromConfig];
            }

            if (!string.IsNullOrWhiteSpace(endpointAddress))
            {
                serviceOptions.EndpointAddress = endpointAddress;
            }

            var version = config[VersionKeyFromConfig];
            if (!string.IsNullOrWhiteSpace(version))
            {
                serviceOptions.ApplicationVersion = version;
            }
        }

        internal static bool IsApplicationInsightsAdded(IServiceCollection services)
        {
            // We treat TelemetryClient as a marker that AI services were added to service collection
            return services.Any(service => service.ServiceType == typeof(TelemetryClient));
        }
    }
}
