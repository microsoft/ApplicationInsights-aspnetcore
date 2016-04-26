﻿namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
    using System.Diagnostics;
    using Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Http;
    using Microsoft.Extensions.DependencyInjection;

    public abstract class TelemetryInitializerBase : ITelemetryInitializer
    {
        private IHttpContextAccessor httpContextAccessor;

        public TelemetryInitializerBase(IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor == null)
            {
                throw new ArgumentNullException("httpContextAccessor");
            }

            this.httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            try
            {
                var context = this.httpContextAccessor.HttpContext;

                if (context == null)
                {
                    AspNetEventSource.Instance.LogTelemetryInitializerBaseInitializeContextNull();
                    return;
                }

                if (context.RequestServices == null)
                {
                    AspNetEventSource.Instance.LogTelemetryInitializerBaseInitializeRequestServicesNull();
                    return;
                }

                var request = context.RequestServices.GetService<RequestTelemetry>();

                if (request == null)
                {
                    AspNetEventSource.Instance.LogTelemetryInitializerBaseInitializeRequestNull();
                    return;
                }

                this.OnInitializeTelemetry(context, request, telemetry);
            }
            catch (Exception exp)
            {
                AspNetEventSource.Instance.LogTelemetryInitializerBaseInitializeException(exp.ToString());
                Debug.WriteLine(exp);
            }
        }

        protected abstract void OnInitializeTelemetry(
            HttpContext platformContext,
            RequestTelemetry requestTelemetry,
            ITelemetry telemetry);
    }
}