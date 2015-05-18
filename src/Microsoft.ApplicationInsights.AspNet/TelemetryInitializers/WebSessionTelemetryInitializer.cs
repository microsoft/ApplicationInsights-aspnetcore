namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http;
    using Microsoft.ApplicationInsights.AspNet.Tracing;

    public class WebSessionTelemetryInitializer : TelemetryInitializerBase
    {
        private const string WebSessionCookieName = "ai_session";

        private readonly AspNet5EventSource eventSource;

        public WebSessionTelemetryInitializer(IHttpContextAccessor httpContextAccessor, AspNet5EventSource eventSource)
             : base(httpContextAccessor, eventSource)
        {
            this.eventSource = eventSource;
        }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (!string.IsNullOrEmpty(telemetry.Context.Session.Id))
            {
                return;
            }

            if (string.IsNullOrEmpty(requestTelemetry.Context.Session.Id))
            {
                UpdateRequestTelemetryFromPlatformContext(requestTelemetry, platformContext);
            }

            if (!string.IsNullOrEmpty(requestTelemetry.Context.Session.Id))
            {
                telemetry.Context.Session.Id = requestTelemetry.Context.Session.Id;
            }
        }

        private void UpdateRequestTelemetryFromPlatformContext(RequestTelemetry requestTelemetry, HttpContext platformContext)
        {
            if (platformContext.Request.Cookies != null && platformContext.Request.Cookies.ContainsKey(WebSessionCookieName))
            {
                var sessionCookieValue = platformContext.Request.Cookies[WebSessionCookieName];
                bool cookieWasRead = false;
                if (!string.IsNullOrEmpty(sessionCookieValue))
                {
                    var sessionCookieParts = sessionCookieValue.Split('|');
                    if (sessionCookieParts.Length > 0)
                    {
                        // Currently SessionContext takes in only SessionId. 
                        // The cookies has SessionAcquisitionDate and SessionRenewDate as well that we are not picking for now.
                        requestTelemetry.Context.Session.Id = sessionCookieParts[0];
                        cookieWasRead = true;
                    }
                }

                if (!cookieWasRead)
                {
                    this.eventSource.ErrorMalformedCookie(WebSessionCookieName, sessionCookieValue);
                }
            }
        }
    }
}