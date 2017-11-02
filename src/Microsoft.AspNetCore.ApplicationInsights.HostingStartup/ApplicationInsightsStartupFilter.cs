// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.SnapshotCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.ApplicationInsights.HostingStartup
{
    internal class ApplicationInsightsLoggerStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                // If user's code also adds the SnapshotCollectorTelemetryProcessor, it will be the last one in TelemetryProcessor list.
                // To make sure user's SnapshotCollectorTelemetryProcessor is used, we scan the list and only enable the last one.
                var telemetryConfiguration = builder.ApplicationServices.GetService<TelemetryConfiguration>();
                if(telemetryConfiguration != null && telemetryConfiguration.TelemetryProcessors != null)
                {
                    SnapshotCollectorTelemetryProcessor lastSnapshotCollectorTelemetryProcessor = null;

                    foreach (var telemetryProcessor in telemetryConfiguration.TelemetryProcessors)
                    {
                        if (telemetryProcessor is SnapshotCollectorTelemetryProcessor snapshotCollectorTelemetryProcessor)
                        {
                            snapshotCollectorTelemetryProcessor.IsEnabled = false;
                            lastSnapshotCollectorTelemetryProcessor = snapshotCollectorTelemetryProcessor;
                        }
                    }

                    if (lastSnapshotCollectorTelemetryProcessor != null)
                    {
                        lastSnapshotCollectorTelemetryProcessor.IsEnabled = true;
                    }
                }

                var loggerFactory = builder.ApplicationServices.GetService<ILoggerFactory>();

                // We need to disable filtering on logger, filtering would be done by LoggerFactory
                var loggerEnabled = true;

                loggerFactory.AddApplicationInsights(
                    builder.ApplicationServices,
                    (s, level) => loggerEnabled,
                    () => loggerEnabled = false);

                next(builder);
            };
        }
    }
}