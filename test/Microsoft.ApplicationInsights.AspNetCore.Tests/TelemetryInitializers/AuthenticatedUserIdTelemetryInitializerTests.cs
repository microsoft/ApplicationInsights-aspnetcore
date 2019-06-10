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
            var ac = BuildHttpContextAccessorWithoutUser(new RequestTelemetry());

            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                ac);

            initializer.Initialize(new EventTelemetry());
        }

        [Fact]
        public void InitializeAssignsAuthenticationIdToRequestTelemetry()
        {
            var requestTelemetry = new RequestTelemetry();

            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                this.BuildHttpContextAccessorWithAuthenticatedUser(requestTelemetry, "TestAuthenticatedId"));

            initializer.Initialize(new EventTelemetry());

            Assert.Equal("TestAuthenticatedId", requestTelemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeAssignsAuthenticationIdToTelemetry()
        {
            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                this.BuildHttpContextAccessorWithAuthenticatedUser(new RequestTelemetry(), "TestAuthenticatedId"));

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("TestAuthenticatedId", telemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeAssignsExistingAuthenticatedUserIdFromRequestTelemetryToTelemetry()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.User.AuthenticatedUserId = "TestAuthenticatedId";

            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                this.BuildHttpContextAccessorWithAuthenticatedUser(requestTelemetry));

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("TestAuthenticatedId", telemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotAssignAuthenticationIdToRequestTelemetryIfAuthenticatedUserIdAlreadySetOnTelemetry()
        {
            var requestTelemetry = new RequestTelemetry();

            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                this.BuildHttpContextAccessorWithAuthenticatedUser(requestTelemetry));

            var telemetry = new EventTelemetry();
            telemetry.Context.User.AuthenticatedUserId = "TestAuthenticatedId";
            initializer.Initialize(telemetry);

            Assert.NotEqual("TestAuthenticatedId", requestTelemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingAuthenticationIdOnRequestTelemetry()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.User.AuthenticatedUserId = "TestAuthenticationId";

            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                this.BuildHttpContextAccessorWithAuthenticatedUser(requestTelemetry, "TestOverriddenAuthenticationId"));

            initializer.Initialize(new EventTelemetry());

            Assert.Equal("TestAuthenticationId", requestTelemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingAuthenticationIdOnTelemetry()
        {
            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                this.BuildHttpContextAccessorWithAuthenticatedUser(new RequestTelemetry(), "TestOverriddenAuthenticationId"));

            var telemetry = new EventTelemetry();
            telemetry.Context.User.AuthenticatedUserId = "TestAuthenticationId";
            initializer.Initialize(telemetry);

            Assert.Equal("TestAuthenticationId", telemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingAuthenticationIdOnRequestTelemetryIfUserNotAuthenticated()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.User.AuthenticatedUserId = "TestAuthenticationId";

            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                this.BuildHttpContextAccessorWithoutUser(requestTelemetry));

            initializer.Initialize(new EventTelemetry());

            Assert.Equal("TestAuthenticationId", requestTelemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingAuthenticationIdOnTelemetryIfUserNotAuthenticated()
        {
            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(),
                this.BuildHttpContextAccessorWithoutUser(new RequestTelemetry()));

            var telemetry = new EventTelemetry();
            telemetry.Context.User.AuthenticatedUserId = "TestAuthenticationId";
            initializer.Initialize(telemetry);

            Assert.Equal("TestAuthenticationId", telemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotAssignAuthenticationIdToRequestTelemetryIfNotEnabled()
        {
            var requestTelemetry = new RequestTelemetry();

            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(false),
                this.BuildHttpContextAccessorWithAuthenticatedUser(requestTelemetry, "TestAuthenticatedId"));

            initializer.Initialize(new EventTelemetry());

            Assert.NotEqual("TestAuthenticatedId", requestTelemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotAssignAuthenticationIdToTelemetryIfNotEnabled()
        {
            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(false),
                this.BuildHttpContextAccessorWithAuthenticatedUser(new RequestTelemetry(), "TestAuthenticatedId"));

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.NotEqual("TestAuthenticatedId", telemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingAuthenticationIdOnRequestTelemetryIfNotEnabled()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.User.AuthenticatedUserId = "TestAuthenticationId";

            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(false),
                this.BuildHttpContextAccessorWithAuthenticatedUser(requestTelemetry, "TestOverriddenAuthenticationId"));

            initializer.Initialize(new EventTelemetry());

            Assert.Equal("TestAuthenticationId", requestTelemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingAuthenticationIdOnTelemetryIfNotEnabled()
        {
            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(false),
                this.BuildHttpContextAccessorWithAuthenticatedUser(new RequestTelemetry(), "TestOverriddenAuthenticationId"));

            var telemetry = new EventTelemetry();
            telemetry.Context.User.AuthenticatedUserId = "TestAuthenticationId";
            initializer.Initialize(telemetry);

            Assert.Equal("TestAuthenticationId", telemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingAuthenticationIdOnRequestTelemetryIfUserNotAuthenticatedAndNotEnabled()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.User.AuthenticatedUserId = "TestAuthenticationId";

            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(false),
                this.BuildHttpContextAccessorWithoutUser(requestTelemetry));

            initializer.Initialize(new EventTelemetry());

            Assert.Equal("TestAuthenticationId", requestTelemetry.Context.User.AuthenticatedUserId);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingAuthenticationIdOnTelemetryIfUserNotAuthenticatedAndNotEnabled()
        {
            var initializer = new AuthenticatedUserIdTelemetryInitializer(
                this.BuildConfigurationWithEnableAuthenticationTracking(false),
                this.BuildHttpContextAccessorWithoutUser(new RequestTelemetry()));

            var telemetry = new EventTelemetry();
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

        private IHttpContextAccessor BuildHttpContextAccessorWithAuthenticatedUser(RequestTelemetry requestTelemetry, string authUserId = "TestDefaultAuthenticationId")
        {
            var userIdentity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, authUserId) },
                "TestAuthenticationType",
                ClaimTypes.Name,
                ClaimTypes.Role);

            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry, null);
            contextAccessor.HttpContext.User = new ClaimsPrincipal(userIdentity);

            return contextAccessor;
        }

        private IHttpContextAccessor BuildHttpContextAccessorWithoutUser(RequestTelemetry requestTelemetry)
        {
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry, null);

            return contextAccessor;
        }
    }
}
