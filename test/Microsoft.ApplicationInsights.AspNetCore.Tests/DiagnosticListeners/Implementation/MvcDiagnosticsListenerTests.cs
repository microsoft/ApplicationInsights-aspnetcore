namespace Microsoft.ApplicationInsights.AspNetCore.Tests.MvcDiagnosticsListener
{
    using DataContracts;
    using DiagnosticListeners;
    using Helpers;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Routing;
    using System.Diagnostics;
    using Xunit;

    public class MvcDiagnosticsListenerTests
    {
        [Fact]
        public void SetsTelemetryNameFromRouteData()
        {
            var actionContext = new ActionContext();
            actionContext.RouteData = new RouteData();
            actionContext.RouteData.Values.Add("controller", "account");
            actionContext.RouteData.Values.Add("action", "login");
            actionContext.RouteData.Values.Add("parameter", "myName");

            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), actionContext);
            string originalTraceIdentifier = contextAccessor.HttpContext.TraceIdentifier;

            var telemetryListener = new DiagnosticListener(TestListenerName);
            var mvcListener = new MvcDiagnosticsListener();
            telemetryListener.SubscribeWithAdapter(mvcListener);
            telemetryListener.Write("Microsoft.AspNetCore.Mvc.BeforeAction",
                new { httpContext = contextAccessor.HttpContext, routeData = actionContext.RouteData });

            var telemetry = contextAccessor.HttpContext.Features.Get<RequestTelemetry>();

            Assert.Equal("GET account/login [parameter]", telemetry.Name);
            Assert.Equal(originalTraceIdentifier + "|" + telemetry.Name, contextAccessor.HttpContext.TraceIdentifier);
        }

        private const string TestListenerName = "TestListener";
    }
}
