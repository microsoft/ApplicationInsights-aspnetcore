namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DiagnosticAdapter;
    using Microsoft.Extensions.Primitives;
    using Extensibility.Implementation.Tracing;

    /// <summary>
    /// <see cref="IApplicationInsightDiagnosticListener"/> implementation that listens for events specific to AspNetCore hosting layer.
    /// </summary>
    internal class HostingDiagnosticListener : IApplicationInsightDiagnosticListener
    {
        private readonly TelemetryClient client;
        private readonly ICorrelationIdLookupHelper correlationIdLookupHelper;
        private readonly string sdkVersion;
        private const string ActivityInitializedByStandardHeaderName = "ActivityInitializedByStandardHeaderName";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:HostingDiagnosticListener"/> class.
        /// </summary>
        /// <param name="client"><see cref="TelemetryClient"/> to post traces to.</param>
        /// <param name="correlationIdLookupHelper">A store for correlation ids that we don't have to query it everytime.</param>
        public HostingDiagnosticListener(TelemetryClient client, ICorrelationIdLookupHelper correlationIdLookupHelper)
        {
            this.client = client;
            this.correlationIdLookupHelper = correlationIdLookupHelper;
            this.sdkVersion = SdkVersionUtils.VersionPrefix + SdkVersionUtils.GetAssemblyVersion();
        }

        /// <inheritdoc/>
        public string ListenerName { get; } = "Microsoft.AspNetCore";

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.HttpRequestIn' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn")]
        public void OnHttpRequestIn()
        {
            // do nothing, just enable the diagnotic source
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.HttpRequestIn.Start' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")]
        public void OnHttpRequestInStart(HttpContext httpContext)
        {
            if (this.client.IsEnabled())
            {
                var requestTelemetry = new RequestTelemetry();

                if (Activity.Current == null)
                {
                    AspNetCoreEventSource.Instance.LogHostingDiagnosticListenerOnHttpRequestInStartActivityNull();
                }
                else if (Activity.Current.ParentId != null)
                {
                    // The activity is initialized from the new "Request-Id" header
                    requestTelemetry.Context.Operation.ParentId = Activity.Current.ParentId;

                    foreach (var prop in Activity.Current.Baggage)
                    {
                        if (!requestTelemetry.Context.Properties.ContainsKey(prop.Key))
                        {
                            requestTelemetry.Context.Properties[prop.Key] = prop.Value;
                        }
                    }
                }
                else
                {
                    // The request doesn't have the new "Request-Id" header but the standard headers,
                    // we create a new activity and initialize it by the standard headers.
                    var xmsRequestRootId = httpContext?.Request?.Headers[RequestResponseHeaders.StandardRootIdHeader];
                    if (xmsRequestRootId.HasValue && xmsRequestRootId.Value.Count > 0)
                    {
                        var activity = new Activity(ActivityInitializedByStandardHeaderName);
                        activity.SetParentId(xmsRequestRootId.Value[0]);
                        activity.Start();
                        httpContext.Features.Set(activity);
                    }

                    var xmsRequestId = httpContext?.Request?.Headers[RequestResponseHeaders.StandardParentIdHeader];
                    if (xmsRequestId.HasValue && xmsRequestId.Value.Count > 0)
                    {
                        requestTelemetry.Context.Operation.ParentId = xmsRequestId.Value[0];
                    }
                }

                requestTelemetry.Id = Activity.Current?.Id;
                requestTelemetry.Context.Operation.Id = Activity.Current?.RootId;

                this.client.Initialize(requestTelemetry);
                requestTelemetry.Start();
                httpContext.Features.Set(requestTelemetry);
                IHeaderDictionary responseHeaders = httpContext.Response?.Headers;
                if (responseHeaders != null &&
                    !string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey) &&
                    (!responseHeaders.ContainsKey(RequestResponseHeaders.RequestContextHeader) || HttpHeadersUtilities.ContainsRequestContextKeyValue(responseHeaders, RequestResponseHeaders.RequestContextTargetKey)))
                {
                    string correlationId = null;
                    if (this.correlationIdLookupHelper.TryGetXComponentCorrelationId(requestTelemetry.Context.InstrumentationKey, out correlationId))
                    {
                        HttpHeadersUtilities.SetRequestContextKeyValue(responseHeaders, RequestResponseHeaders.RequestContextTargetKey, correlationId);
                    }
                }
            }
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop")]
        public void OnHttpRequestInStop(HttpContext httpContext)
        {
            EndRequest(httpContext);
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.UnhandledException' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
        public void OnHostingException(HttpContext httpContext, Exception exception)
        {
            this.OnException(httpContext, exception);
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.HandledException' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.HandledException")]
        public void OnDiagnosticsHandledException(HttpContext httpContext, Exception exception)
        {
            this.OnException(httpContext, exception);
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Diagnostics.UnhandledException' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.UnhandledException")]
        public void OnDiagnosticsUnhandledException(HttpContext httpContext, Exception exception)
        {
            this.OnException(httpContext, exception);
        }

        private void EndRequest(HttpContext httpContext)
        {
            if (this.client.IsEnabled())
            {
                var telemetry = httpContext?.Features.Get<RequestTelemetry>();

                if (telemetry == null)
                {
                    return;
                }

                telemetry.Stop();
                telemetry.ResponseCode = httpContext.Response.StatusCode.ToString();

                var successExitCode = httpContext.Response.StatusCode < 400;
                if (telemetry.Success == null)
                {
                    telemetry.Success = successExitCode;
                }
                else
                {
                    telemetry.Success &= successExitCode;
                }

                if (string.IsNullOrEmpty(telemetry.Name))
                {
                    telemetry.Name = httpContext.Request.Method + " " + httpContext.Request.Path.Value;
                }

                telemetry.HttpMethod = httpContext.Request.Method;
                telemetry.Url = httpContext.Request.GetUri();
                telemetry.Context.GetInternalContext().SdkVersion = this.sdkVersion;
                this.client.TrackRequest(telemetry);

                var activity = httpContext?.Features.Get<Activity>();
                if (activity != null && activity.OperationName == ActivityInitializedByStandardHeaderName)
                {
                    activity.Stop();
                }
            }
        }

        private void OnException(HttpContext httpContext, Exception exception)
        {
            if (this.client.IsEnabled())
            {
                var telemetry = httpContext?.Features.Get<RequestTelemetry>();
                if (telemetry != null)
                {
                    telemetry.Success = false;
                }

                var exceptionTelemetry = new ExceptionTelemetry(exception);
                exceptionTelemetry.HandledAt = ExceptionHandledAt.Platform;
                exceptionTelemetry.Context.GetInternalContext().SdkVersion = this.sdkVersion;
                this.client.Track(exceptionTelemetry);
            }
        }
    }
}