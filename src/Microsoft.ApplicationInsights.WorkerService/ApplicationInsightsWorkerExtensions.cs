using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WindowsServer;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> that allow adding Application Insights services to application.
    /// </summary>
    public static class ApplicationInsightsWorkerExtensions
    {
        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetryNonWeb(this IServiceCollection services)
        {
            try
            {
                if (!ApplicationInsightsExtensionsCommon.IsApplicationInsightsAdded(services))
                {
                    services
                        .AddSingleton<ITelemetryInitializer, ApplicationInsights.AspNetCore.TelemetryInitializers.
                            DomainNameRoleInstanceTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, ComponentVersionTelemetryInitializer>();
                    // services.AddSingleton<ITelemetryInitializer, AspNetCoreEnvironmentTelemetryInitializer>();
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

                        module.EnableW3CHeadersInjection = true;
                    });

                    services.AddSingleton<ITelemetryModule, PerformanceCollectorModule>();
                    services.AddSingleton<ITelemetryModule, AppServicesHeartbeatTelemetryModule>();
                    services.AddSingleton<ITelemetryModule, AzureInstanceMetadataTelemetryModule>();
                    services.AddSingleton<ITelemetryModule, QuickPulseTelemetryModule>();
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
                    services.AddSingleton<TelemetryConfiguration>(provider =>
                        provider.GetService<IOptions<TelemetryConfiguration>>().Value);

                    services.TryAddSingleton<IApplicationIdProvider, ApplicationInsightsApplicationIdProvider>();

                    services.AddSingleton<TelemetryClient>();

                    /*services
                        .TryAddSingleton<IConfigureOptions<ApplicationInsightsServiceOptions>,
                            DefaultApplicationInsightsServiceConfigureOptions>();
                            */

                    services.AddOptions();
                    services.AddSingleton<IOptions<TelemetryConfiguration>, TelemetryConfigurationOptions>();
                    services
                        .AddSingleton<IConfigureOptions<TelemetryConfiguration>, TelemetryConfigurationOptionsSetup>();

                    services.AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.AddApplicationInsights();

                        // The default behavior is to capture only logs above Warning level from all categories.
                        // This can achieved with this code level filter -> loggingBuilder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("",LogLevel.Warning);
                        // However, this will make it impossible to override this behavior from Configuration like below using appsettings.json:
                        // {
                        //   "Logging": {
                        //     "ApplicationInsights": {
                        //       "LogLevel": {
                        //         "": "Error"
                        //       }
                        //     }
                        //   },
                        //   ...
                        // }
                        // The reason is as both rules will match the filter, the last one added wins.
                        // To ensure that the default filter is in the beginning of filter rules, so that user override from Configuration will always win,
                        // we add code filter rule to the 0th position as below.
                        loggingBuilder.Services.Configure<LoggerFilterOptions>(
                            options => options.Rules.Insert(
                                0,
                                new LoggerFilterRule(
                                    "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider", null,
                                    LogLevel.Warning, null)));
                    });
                }

                return services;
            }
            catch (Exception e)
            {
                // AspNetCoreEventSource.Instance.LogError(e.ToInvariantString());
                return services;
            }
        }
    }
}
