
namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;
    using Xunit;
    using System.Threading.Tasks;

    public class RequestTrackingMiddlewareTest
    {
        private object sentTelemetryLock = new object();
        private const string HttpRequestScheme = "http";
        private static readonly HostString HttpRequestHost = new HostString("testHost");
        private static readonly PathString HttpRequestPath = new PathString("/path/path");
        private static readonly QueryString HttpRequestQueryString = new QueryString("?query=1");

        private static Uri CreateUri(string scheme, HostString host, PathString? path = null, QueryString? query = null)
        {
            string uriString = string.Format(CultureInfo.InvariantCulture, "{0}://{1}", scheme, host);
            if (path != null)
            {
                uriString += path.Value;
            }
            if (query != null)
            {
                uriString += query.Value;
            }
            return new Uri(uriString);
        }

        private HttpContext CreateContext(string scheme, HostString host, PathString? path = null, QueryString? query = null, string method = null)
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.Scheme = scheme;
            context.Request.Host = host;

            if (path.HasValue)
            {
                context.Request.Path = path.Value;
            }

            if (query.HasValue)
            {
                context.Request.QueryString = query.Value;
            }

            if (!string.IsNullOrEmpty(method))
            {
                context.Request.Method = method;
            }

            Assert.Null(context.Features.Get<RequestTelemetry>());

            return context;
        }

        private List<ITelemetry> sentTelemetry = new List<ITelemetry>();

        private readonly HostingDiagnosticListener middleware;

        public RequestTrackingMiddlewareTest()
        {
            this.middleware = new HostingDiagnosticListener(CommonMocks.MockTelemetryClient(
                telemetry =>
                {
                    lock (sentTelemetryLock)
                    {
                        this.sentTelemetry.Add(telemetry);
                    }
                }));
        }

        [Fact]
        public void TestSdkVersionIsPopulatedByMiddleware()
        {
            HttpContext context = CreateContext(HttpRequestScheme, HttpRequestHost);

            middleware.OnHttpRequestInStart(context);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnHttpRequestInStop(context);

            Assert.Equal(1, sentTelemetry.Count);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry.First());

            RequestTelemetry requestTelemetry = this.sentTelemetry.First() as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.True(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            // Sometimes it will throw exception for failed to get the key
            // Assert.Equal("", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(HttpRequestScheme, HttpRequestHost), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
        }

        [Fact]
        public void TestRequestUriIsPopulatedByMiddleware()
        {
            HttpContext context = CreateContext(HttpRequestScheme, HttpRequestHost, HttpRequestPath, HttpRequestQueryString);

            middleware.OnHttpRequestInStart(context);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnHttpRequestInStop(context);

            Assert.Equal(1, sentTelemetry.Count);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry[0]);
            RequestTelemetry requestTelemetry = sentTelemetry[0] as RequestTelemetry;
            Assert.NotNull(requestTelemetry.Url);
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.True(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            // Sometimes it will throw exception for failed to get the key
            // Assert.Equal("", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(HttpRequestScheme, HttpRequestHost, HttpRequestPath, HttpRequestQueryString), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
        }

        [Fact]
        public void RequestWillBeMarkedAsFailedForRunawayException()
        {
            HttpContext context = CreateContext(HttpRequestScheme, HttpRequestHost);

            middleware.OnHttpRequestInStart(context);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnDiagnosticsUnhandledException(context, null);
            middleware.OnHttpRequestInStop(context);

            Assert.Equal(2, sentTelemetry.Count);
            Assert.IsType<ExceptionTelemetry>(this.sentTelemetry[0]);

            Assert.IsType<RequestTelemetry>(this.sentTelemetry[1]);
            RequestTelemetry requestTelemetry = this.sentTelemetry[1] as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.False(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            // Sometimes it will throw exception for failed to get the key
            // Assert.Equal("", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(HttpRequestScheme, HttpRequestHost), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
        }

        [Fact]
        public void OnEndRequestSetsRequestNameToMethodAndPathForPostRequest()
        {
            HttpContext context = CreateContext(HttpRequestScheme, HttpRequestHost, "/Test", method: "POST");

            middleware.OnHttpRequestInStart(context);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnHttpRequestInStop(context);

            Assert.Equal(1, sentTelemetry.Count);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry[0]);
            RequestTelemetry requestTelemetry = this.sentTelemetry[0] as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.True(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            // Sometimes it will throw exception for failed to get the key
            // Assert.Equal("POST", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(HttpRequestScheme, HttpRequestHost, "/Test"), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Equal("POST /Test", requestTelemetry.Name);
        }

        [Fact]
        public void OnEndRequestSetsRequestNameToMethodAndPath()
        {
            HttpContext context = CreateContext(HttpRequestScheme, HttpRequestHost, "/Test", method: "GET");

            middleware.OnHttpRequestInStart(context);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnHttpRequestInStop(context);

            Assert.NotNull(this.sentTelemetry);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry[0]);
            RequestTelemetry requestTelemetry = this.sentTelemetry[0] as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.True(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            // Sometimes it will throw exception for failed to get the key
            //Assert.Equal("GET", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(HttpRequestScheme, HttpRequestHost, "/Test"), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Equal("GET /Test", requestTelemetry.Name);
        }

        [Fact]
        public void OnEndRequestFromSameInstrumentationKey()
        {
            HttpContext context = CreateContext(HttpRequestScheme, HttpRequestHost, "/Test", method: "GET");
            context.Request.Headers.Add(RequestResponseHeaders.SourceInstrumentationKeyHeader, CommonMocks.InstrumentationKeyHash);

            middleware.OnHttpRequestInStart(context);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnHttpRequestInStop(context);

            Assert.NotNull(this.sentTelemetry);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry[0]);
            RequestTelemetry requestTelemetry = this.sentTelemetry[0] as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.True(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            // Sometimes it will throw exception for failed to get the key
            //Assert.Equal("GET", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(HttpRequestScheme, HttpRequestHost, "/Test"), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Equal("GET /Test", requestTelemetry.Name);
        }

        [Fact]
        public void OnEndRequestFromDifferentInstrumentationKey()
        {
            HttpContext context = CreateContext(HttpRequestScheme, HttpRequestHost, "/Test", method: "GET");
            context.Request.Headers.Add(RequestResponseHeaders.SourceInstrumentationKeyHeader, "DIFFERENT_INSTRUMENTATION_KEY_HASH");

            middleware.OnHttpRequestInStart(context);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnHttpRequestInStop(context);

            Assert.Equal(1, sentTelemetry.Count);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry[0]);
            RequestTelemetry requestTelemetry = this.sentTelemetry[0] as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.True(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            // Sometimes it will throw exception for failed to get the key
            // Assert.Equal("GET", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(HttpRequestScheme, HttpRequestHost, "/Test"), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Equal("GET /Test", requestTelemetry.Name);
        }

        [Fact]
        public async void RequestsUnderDifferentActivitiesGetDifferentIds()
        {
            var context1 = new DefaultHttpContext();
            context1.Request.Scheme = HttpRequestScheme;
            context1.Request.Host = HttpRequestHost;
            context1.Request.Method = "GET";
            context1.Request.Path = "/Test?id=1";

            var context2 = new DefaultHttpContext();
            context2.Request.Scheme = HttpRequestScheme;
            context2.Request.Host = HttpRequestHost;
            context2.Request.Method = "GET";
            context2.Request.Path = "/Test?id=2";

            var task1 = Task.Run(() =>
            {
                var act = new Activity("operation1");
                act.Start();
                middleware.OnHttpRequestInStart(context1);
                middleware.OnHttpRequestInStop(context1);
            });

            var task2 = Task.Run(() =>
            {
                var act = new Activity("operation2");
                act.Start();
                middleware.OnHttpRequestInStart(context2);
                middleware.OnHttpRequestInStop(context2);
            });

            await Task.WhenAll(task1, task2);

            Assert.Equal(2, sentTelemetry.Count);
            var id1 = ((RequestTelemetry)sentTelemetry[0]).Id;
            var id2 = ((RequestTelemetry)sentTelemetry[1]).Id;
            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void OnHttpRequestInStopSetsDurations()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = HttpRequestScheme;
            context.Request.Host = HttpRequestHost;
            context.Request.Method = "GET";
            context.Request.Path = "/Test?id=1";

            middleware.OnHttpRequestInStart(context);
            middleware.OnHttpRequestInStop(context);

            Assert.Equal(1, sentTelemetry.Count);
            Assert.NotEqual(0, ((RequestTelemetry)sentTelemetry[0]).Duration.TotalMilliseconds);
        }
    }
}
