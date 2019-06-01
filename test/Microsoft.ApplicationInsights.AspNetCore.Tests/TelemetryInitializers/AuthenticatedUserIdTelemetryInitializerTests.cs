namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using System;
    using System.Security.Claims;
    using Xunit;

    public class AuthenticatedUserIdTelemetryInitializerTests
    {
        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.ThrowsAny<ArgumentNullException>(() =>
            {
                var initializer = new AuthenticatedUserIdTelemetryInitializer(
                    this.BuildConfigurationWithEnableAuthenticationTracking(),
                    null);
            });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor()
            {
                HttpContext = null
            };

            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor()
            {
                HttpContext = new DefaultHttpContext()
            };

            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfUserIsUnavailable()
        {
            var ac = BuildHttpContextAccessorWithoutUser();

            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeAssignsAuthenticationIdToTelemetry()
        {
            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                this.BuildHttpContextAccessorWithAuthenticatedUser("TestAuthenticatedId"));

            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("TestAuthenticatedId", telemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingAuthenticationId()
        {
            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                this.BuildHttpContextAccessorWithAuthenticatedUser("TestOverriddenAuthenticationId"));

            var telemetry = new RequestTelemetry();
            telemetry.Context.User.AuthenticatedUserId = "TestAuthenticationId";
            initializer.Initialize(telemetry);

            Assert.Equal("TestAuthenticationId", telemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingAuthenticationIdIfUserNotAuthenticated()
        {
            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                this.BuildHttpContextAccessorWithoutUser());

            var telemetry = new RequestTelemetry();
            telemetry.Context.User.AuthenticatedUserId = "TestAuthenticationId";
            initializer.Initialize(telemetry);

            Assert.Equal("TestAuthenticationId", telemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotAssignAuthenticationIdToTelemetryIfNotEnabled()
        {
            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(false),
                this.BuildHttpContextAccessorWithAuthenticatedUser("TestAuthenticatedId"));

            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);

            Assert.NotEqual("TestAuthenticatedId", telemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingAuthenticationIdIfNotEnabled()
        {
            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(false),
                this.BuildHttpContextAccessorWithAuthenticatedUser("TestOverriddenAuthenticationId"));

            var telemetry = new RequestTelemetry();
            telemetry.Context.User.AuthenticatedUserId = "TestAuthenticationId";
            initializer.Initialize(telemetry);

            Assert.Equal("TestAuthenticationId", telemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingAuthenticationIdIfUserNotAuthenticatedAndNotEnabled()
        {
            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(false),
                this.BuildHttpContextAccessorWithoutUser());

            var telemetry = new RequestTelemetry();
            telemetry.Context.User.AuthenticatedUserId = "TestAuthenticationId";
            initializer.Initialize(telemetry);

            Assert.Equal("TestAuthenticationId", telemetry.Context.User.AuthenticatedUserId);
        }

        private IOptions<ApplicationInsightsServiceOptions> BuildConfigurationWithEnableAuthenticationTracking(bool enableAuthenticationTracking = true)
        {
            return new OptionsWrapper<ApplicationInsightsServiceOptions>(new ApplicationInsightsServiceOptions()
            {
                EnableAuthenticationTracking = enableAuthenticationTracking
            });
        }

        private IHttpContextAccessor BuildHttpContextAccessorWithAuthenticatedUser(string authUserId = "TestDefaultAuthenticationId")
        {
            var userIdentity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, authUserId) },
                "TestAuthenticationType",
                ClaimTypes.Name,
                ClaimTypes.Role);

            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);
            contextAccessor.HttpContext.User = new ClaimsPrincipal(userIdentity);

            return contextAccessor;
        }

        private IHttpContextAccessor BuildHttpContextAccessorWithoutUser()
        {
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);

            return contextAccessor;
        }
    }
}
