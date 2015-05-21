namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using System;
    using System.Diagnostics.Tracing;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.AspNet.Tracing;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http;
    using Microsoft.AspNet.Http.Core;
    using Xunit;

    public class TelemetryInitializerBaseTest
    {
        private class TelemetryInitializerMock : TelemetryInitializerBase
        {
            public TelemetryInitializerMock(IHttpContextAccessor httpContextAccessor, AspNet5EventSource eventSource)
                : base(httpContextAccessor, eventSource)
            {

            }

            protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
            {
                //do nothing
            }
        }

        [Fact]
        public void InitializeDoesNotThrowAndTracesVerboseMessageIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };

            var eventSource = new Tracing.AspNet5EventSource();

            using (var eventListener = new AspNet5EventTestListener(eventSource))
            {
                var initializer = new TelemetryInitializerMock(ac, eventSource);

                initializer.Initialize(new RequestTelemetry());

                Assert.Equal(1, eventListener.Events.Count);
                Assert.Contains("HttpContext is null", eventListener.Events[0].Message);
                Assert.Equal(EventLevel.Verbose, eventListener.Events[0].Level);
            }
        }

        [Fact]
        public void InitializeDoesNotThrowAndTracesVerboseMessageIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };

            var eventSource = new Tracing.AspNet5EventSource();

            using (var eventListener = new AspNet5EventTestListener(eventSource))
            {
                var initializer = new TelemetryInitializerMock(ac, eventSource);

                initializer.Initialize(new RequestTelemetry());

                Assert.Equal(1, eventListener.Events.Count);
                Assert.Contains("RequestServices are not available", eventListener.Events[0].Message);
                Assert.Equal(EventLevel.Verbose, eventListener.Events[0].Level);
            }
        }
    }
}
