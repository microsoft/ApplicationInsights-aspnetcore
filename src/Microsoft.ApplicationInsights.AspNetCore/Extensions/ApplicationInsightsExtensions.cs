namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.AspNetCore.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
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
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
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
        [SuppressMessage(category: "", checkId: "CS1591:MissingXmlComment", Justification = "Obsolete method.")]
        [Obsolete("This middleware is no longer needed. Enable Request monitoring using services.AddApplicationInsights")]
        public static IApplicationBuilder UseApplicationInsightsRequestTelemetry(this IApplicationBuilder app)
        {
            return app;
        }

        [SuppressMessage(category: "", checkId: "CS1591:MissingXmlComment", Justification = "Obsolete method.")]
        [Obsolete("This middleware is no longer needed to track exceptions as they are automatically tracked by RequestTrackingTelemetryModule")]
        public static IApplicationBuilder UseApplicationInsightsExceptionTelemetry(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionTrackingMiddleware>();
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="instrumentationKey">Instrumentation key to use for telemetry.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(
            this IServiceCollection services,
            string instrumentationKey)
        {
            services.AddApplicationInsightsTelemetry(options => options.InstrumentationKey = instrumentationKey);
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configuration">Configuration to use for sending telemetry.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddApplicationInsightsTelemetry(options => AddTelemetryConfiguration(configuration, options));
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="options">The action used to configure the options.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(
            this IServiceCollection services,
            Action<ApplicationInsightsServiceOptions> options)
        {
            services.AddApplicationInsightsTelemetry();
            services.Configure(options);
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="options">The options instance used to configure with.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(
            this IServiceCollection services,
            ApplicationInsightsServiceOptions options)
        {
            services.AddApplicationInsightsTelemetry();
            services.Configure((ApplicationInsightsServiceOptions o) =>
            {
                o.ApplicationVersion = options.ApplicationVersion;
                o.DeveloperMode = options.DeveloperMode;
                o.EnableAdaptiveSampling = options.EnableAdaptiveSampling;
                o.EnableAuthenticationTrackingJavaScript = options.EnableAuthenticationTrackingJavaScript;
                o.EnableDebugLogger = options.EnableDebugLogger;
                o.EnableQuickPulseMetricStream = options.EnableQuickPulseMetricStream;
                o.EndpointAddress = options.EndpointAddress;
                o.InstrumentationKey = options.InstrumentationKey;
                o.EnableHeartbeat = options.EnableHeartbeat;
                o.AddAutoCollectedMetricExtractor = options.AddAutoCollectedMetricExtractor;
            });
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services)
        {
            try
            {
                if (!IsApplicationInsightsAdded(services))
                {
                    services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                    services
                        .AddSingleton<ITelemetryInitializer, ApplicationInsights.AspNetCore.TelemetryInitializers.
                            DomainNameRoleInstanceTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, AzureWebAppRoleEnvironmentTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, ComponentVersionTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, ClientIpHeaderTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, OperationNameTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, SyntheticTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, WebSessionTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, WebUserTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, AspNetCoreEnvironmentTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, HttpDependenciesParsingTelemetryInitializer>();
                    services.TryAddSingleton<ITelemetryChannel, ServerTelemetryChannel>();

                    services.AddSingleton<ITelemetryModule, DependencyTrackingTelemetryModule>();
                    services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
                    {
                        module.EnableLegacyCorrelationHeadersInjection =
                            o.DependencyCollectionOptions.EnableLegacyCorrelationHeadersInjection;

                        var excludedDomains = module.ExcludeComponentCorrelationHttpHeadersOnDomains;
                        excludedDomains.Add("core.windows.net");
                        excludedDomains.Add("core.chinacloudapi.cn");
                        excludedDomains.Add("core.cloudapi.de");
                        excludedDomains.Add("core.usgovcloudapi.net");

                        if (module.EnableLegacyCorrelationHeadersInjection)
                        {
                            excludedDomains.Add("localhost");
                            excludedDomains.Add("127.0.0.1");
                        }

                        var includedActivities = module.IncludeDiagnosticSourceActivities;
                        includedActivities.Add("Microsoft.Azure.EventHubs");
                        includedActivities.Add("Microsoft.Azure.ServiceBus");
                    });

                    services.ConfigureTelemetryModule<RequestTrackingTelemetryModule>((module, options) =>
                    {
                        module.CollectionOptions = options.RequestCollectionOptions;
                    });

                    services.AddSingleton<ITelemetryModule, PerformanceCollectorModule>();
                    services.AddSingleton<ITelemetryModule, AppServicesHeartbeatTelemetryModule>();
                    services.AddSingleton<ITelemetryModule, AzureInstanceMetadataTelemetryModule>();
                    services.AddSingleton<ITelemetryModule, QuickPulseTelemetryModule>();
                    services.AddSingleton<ITelemetryModule, RequestTrackingTelemetryModule>();
#if NETSTANDARD2_0
                    services.AddSingleton<ITelemetryModule, EventCounterCollectionModule>();
                    services.ConfigureTelemetryModule<EventCounterCollectionModule>((eventCounterModule, options) =>
                    {
                        // Ref this code for actual names. https://github.com/dotnet/coreclr/blob/dbc5b56c48ce30635ee8192c9814c7de998043d5/src/System.Private.CoreLib/src/System/Diagnostics/Eventing/RuntimeEventSource.cs
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "cpu-usage"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "working-set"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "gc-heap-size"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "gen-0-gc-count"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "gen-1-gc-count"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "gen-2-gc-count"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "time-in-gc"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "gen-0-size"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "gen-1-size"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "gen-2-size"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "loh-size"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "alloc-rate"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "assembly-count"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "exception-count"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "threadpool-thread-count"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "monitor-lock-contention-count"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "threadpool-queue-length"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "threadpool-completed-items-count"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "active-timer-count"));

                        // Ref this code for actual names. https://github.com/aspnet/AspNetCore/blob/f3f9a1cdbcd06b298035b523732b9f45b1408461/src/Hosting/Hosting/src/Internal/HostingEventSource.cs
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("Microsoft.AspNetCore.Hosting", "requests-per-second"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("Microsoft.AspNetCore.Hosting", "total-requests"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("Microsoft.AspNetCore.Hosting", "current-requests"));
                        eventCounterModule.Counters.Add(new EventCounterCollectionRequest("Microsoft.AspNetCore.Hosting", "failed-requests"));
                    });
#endif
                    services.AddSingleton<TelemetryConfiguration>(provider =>
                        provider.GetService<IOptions<TelemetryConfiguration>>().Value);

                    services.TryAddSingleton<IApplicationIdProvider, ApplicationInsightsApplicationIdProvider>();

                    services.AddSingleton<TelemetryClient>();

                    services
                        .TryAddSingleton<IConfigureOptions<ApplicationInsightsServiceOptions>,
                            DefaultApplicationInsightsServiceConfigureOptions>();

                    // Using startup filter instead of starting DiagnosticListeners directly because
                    // AspNetCoreHostingDiagnosticListener injects TelemetryClient that injects TelemetryConfiguration
                    // that requires IOptions infrastructure to run and initialize
                    services.AddSingleton<IStartupFilter, ApplicationInsightsStartupFilter>();
                    services.AddSingleton<IJavaScriptSnippet, JavaScriptSnippet>();
                    services.AddSingleton<JavaScriptSnippet>(); // Add 'JavaScriptSnippet' "Service" for backwards compatibility. To remove in favour of 'IJavaScriptSnippet'.

                    services.AddOptions();
                    services.AddSingleton<IOptions<TelemetryConfiguration>, TelemetryConfigurationOptions>();
                    services
                        .AddSingleton<IConfigureOptions<TelemetryConfiguration>, TelemetryConfigurationOptionsSetup>();

                    // NetStandard2.0 has a package reference to Microsoft.Extensions.Logging.ApplicationInsights, and
                    // enables ApplicationInsightsLoggerProvider by default.
#if NETSTANDARD2_0
                    AddApplicationInsightsLoggerProvider(services);
#endif
                }

                return services;
            }
            catch (Exception e)
            {
                AspNetCoreEventSource.Instance.LogError(e.ToInvariantString());
                return services;
            }
        }
    }
}
