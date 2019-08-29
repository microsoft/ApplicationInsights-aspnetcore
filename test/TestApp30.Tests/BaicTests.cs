using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace TestApp30.Tests
{
    public class BaicTests : IClassFixture<CustomWebApplicationFactory<TestApp30.Startup>>
    {
        private readonly CustomWebApplicationFactory<TestApp30.Startup> _factory;

        public BaicTests(CustomWebApplicationFactory<TestApp30.Startup> factory)
        {
            _factory = factory;
            _factory.sentItems.Clear();
        }

        [Fact]
        public async Task RequestSuccessWithTraceParent()
        {
            // Arrange
            var client = _factory.CreateClient();
            var url = "/Home/Index";

            // Act
            client.DefaultRequestHeaders.Add("traceparent", "00-4e3083444c10254ba40513c7316332eb-e2a5f830c0ee2c46-00");
            var response = await client.GetAsync(url);
            Task.Delay(3000).Wait();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8",
                response.Content.Headers.ContentType.ToString());

            var items = _factory.sentItems;
            // 1 Trace from Ilogger, 1 Request
            Assert.Equal(2, items.Count);

            var req = GetFirstTelemetryOfType<RequestTelemetry>(items);
            var trace = GetFirstTelemetryOfType<TraceTelemetry>(items);
            Assert.NotNull(req);
            Assert.NotNull(trace);

            Assert.Equal("4e3083444c10254ba40513c7316332eb", req.Context.Operation.Id);
            Assert.Equal("00-4e3083444c10254ba40513c7316332eb-e2a5f830c0ee2c46-00", req.Context.Operation.ParentId);
            Assert.Equal("4e3083444c10254ba40513c7316332eb", trace.Context.Operation.Id);
            Assert.Contains("|4e3083444c10254ba40513c7316332eb.", req.Id);
            Assert.Equal(req.Id, trace.Context.Operation.ParentId);

            Assert.Equal("http://localhost" + url, req.Url.ToString());
            Assert.True(req.Success);
        }

        [Fact]
        public async Task RequestFailedWithTraceParent()
        {
            // Arrange
            var client = _factory.CreateClient();
            var url = "/Home/Error";

            // Act
            client.DefaultRequestHeaders.Add("traceparent", "00-4e3083444c10254ba40513c7316332eb-e2a5f830c0ee2c46-00");
            var response = await client.GetAsync(url);
            Task.Delay(3000).Wait();

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            
            var items = _factory.sentItems;
            Assert.Equal(2, items.Count);

            var req = GetFirstTelemetryOfType<RequestTelemetry>(items);
            var exc = GetFirstTelemetryOfType<ExceptionTelemetry>(items);
            Assert.NotNull(req);
            Assert.NotNull(exc);

            Assert.Equal("4e3083444c10254ba40513c7316332eb", req.Context.Operation.Id);
            Assert.Equal("4e3083444c10254ba40513c7316332eb", exc.Context.Operation.Id);
            Assert.Equal("00-4e3083444c10254ba40513c7316332eb-e2a5f830c0ee2c46-00", req.Context.Operation.ParentId);
            Assert.Equal(req.Id, exc.Context.Operation.ParentId);
            Assert.Contains("|4e3083444c10254ba40513c7316332eb.", req.Id);

            Assert.Equal("http://localhost" + url, req.Url.ToString());
            Assert.False(req.Success);
        }

        [Fact]
        public async Task RequestSuccessWithW3CCompatibleRequestId()
        {
            // Arrange
            var client = _factory.CreateClient();
            var url = "/Home/Index";

            // Act
            client.DefaultRequestHeaders.Add("request-id", "|40d1a5a08a68c0998e4a3b7c91915ca6.b9e41c35_1.");
            var response = await client.GetAsync(url);
            Task.Delay(3000).Wait();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8",
                response.Content.Headers.ContentType.ToString());

            var items = _factory.sentItems;
            // 1 Trace from Ilogger, 1 Request
            Assert.Equal(2, items.Count);

            var req = GetFirstTelemetryOfType<RequestTelemetry>(items);
            var trace = GetFirstTelemetryOfType<TraceTelemetry>(items);
            Assert.NotNull(req);
            Assert.NotNull(trace);

            Assert.Equal("40d1a5a08a68c0998e4a3b7c91915ca6", req.Context.Operation.Id);
            Assert.Equal("40d1a5a08a68c0998e4a3b7c91915ca6", trace.Context.Operation.Id);

            Assert.Equal("|40d1a5a08a68c0998e4a3b7c91915ca6.b9e41c35_1.", req.Context.Operation.ParentId);
            Assert.Contains("|40d1a5a08a68c0998e4a3b7c91915ca6.", req.Id);
            Assert.Equal(req.Id, trace.Context.Operation.ParentId);

            Assert.Equal("http://localhost" + url, req.Url.ToString());
            Assert.True(req.Success);
        }

        [Fact]
        public async Task RequestFailedWithW3CCompatibleRequestId()
        {
            // Arrange
            var client = _factory.CreateClient();
            var url = "/Home/Error";

            // Act
            client.DefaultRequestHeaders.Add("request-id", "|40d1a5a08a68c0998e4a3b7c91915ca6.b9e41c35_1.");
            var response = await client.GetAsync(url);
            Task.Delay(3000).Wait();

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var items = _factory.sentItems;
            Assert.Equal(2, items.Count);

            var req = GetFirstTelemetryOfType<RequestTelemetry>(items);
            var exc = GetFirstTelemetryOfType<ExceptionTelemetry>(items);
            Assert.NotNull(req);
            Assert.NotNull(exc);

            Assert.Equal("40d1a5a08a68c0998e4a3b7c91915ca6", req.Context.Operation.Id);
            Assert.Equal("40d1a5a08a68c0998e4a3b7c91915ca6", exc.Context.Operation.Id);
            
            Assert.Equal(req.Id, exc.Context.Operation.ParentId);
            Assert.Equal("|40d1a5a08a68c0998e4a3b7c91915ca6.b9e41c35_1.", req.Context.Operation.ParentId);
            Assert.Contains("|40d1a5a08a68c0998e4a3b7c91915ca6.", req.Id);

            Assert.Equal("http://localhost" + url, req.Url.ToString());
            Assert.False(req.Success);
        }

        [Fact]
        public async Task RequestSuccessWithNonW3CCompatibleRequestId()
        {
            // Arrange
            var client = _factory.CreateClient();
            var url = "/Home/Index";

            // Act
            client.DefaultRequestHeaders.Add("request-id", "|noncompatible.b9e41c35_1.");
            var response = await client.GetAsync(url);
            Task.Delay(3000).Wait();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8",
                response.Content.Headers.ContentType.ToString());

            var items = _factory.sentItems;
            // 1 Trace from Ilogger, 1 Request
            Assert.Equal(2, items.Count);

            var req = GetFirstTelemetryOfType<RequestTelemetry>(items);
            var trace = GetFirstTelemetryOfType<TraceTelemetry>(items);
            Assert.NotNull(req);
            Assert.NotNull(trace);

            Assert.NotEqual("noncompatible", req.Context.Operation.Id);
            Assert.NotEqual("noncompatible", trace.Context.Operation.Id);

            Assert.Equal("|noncompatible.b9e41c35_1.", req.Context.Operation.ParentId);
            Assert.Contains($"|{req.Context.Operation.Id}.", req.Id);
            Assert.Equal(req.Id, trace.Context.Operation.ParentId);
            Assert.Equal("noncompatible", req.Properties["ai_legacyRootId"]);

            Assert.Equal("http://localhost" + url, req.Url.ToString());
            Assert.True(req.Success);
        }

        private T GetFirstTelemetryOfType<T>(IList<ITelemetry> items)
        {
            foreach(var item in items)
            {
                if(item is T)
                {
                    return (T) item;
                }
            }

            return default(T);
        }

    }
}
