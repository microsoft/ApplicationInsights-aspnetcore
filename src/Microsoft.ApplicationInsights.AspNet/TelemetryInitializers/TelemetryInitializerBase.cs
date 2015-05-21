namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
    using System.Diagnostics;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http;
    using Microsoft.Framework.DependencyInjection;
    using Microsoft.ApplicationInsights.AspNet.Tracing;

    public abstract class TelemetryInitializerBase : ITelemetryInitializer
    {
        private IHttpContextAccessor httpContextAccessor;

        private readonly AspNet5EventSource eventSource;

        public TelemetryInitializerBase(IHttpContextAccessor httpContextAccessor, AspNet5EventSource eventSource)
        {
            if (httpContextAccessor == null)
            {
                throw new ArgumentNullException("httpContextAccessor");
            }

            this.httpContextAccessor = httpContextAccessor;

            //Event source may never be null as it will be injected by DI.
            this.eventSource = eventSource;
        }

        public void Initialize(ITelemetry telemetry)
        {
            try
            {
                var context = this.httpContextAccessor.HttpContext;
                if (context == null)
                {
                    this.eventSource.TelemetryInitializerNotEnabledOnHttpContextNull();
                    return;
                }

                if (context.RequestServices == null)
                {
                    this.eventSource.TelemetryInitializerNotEnabledOnRequestServicesNull();
                    return;
                }

                var request = context.RequestServices.GetService<RequestTelemetry>();
                if (request == null)
                {
                    this.eventSource.RegisterApplicationInsightsServices();
                    return;
                }

                this.OnInitializeTelemetry(context, request, telemetry);
            }
            catch (Exception exp)
            {
                this.eventSource.TelemetryInitializerFailedToCollectData(exp);
            }
        }

        protected abstract void OnInitializeTelemetry(
            HttpContext platformContext,
            RequestTelemetry requestTelemetry,
            ITelemetry telemetry);
    }
}