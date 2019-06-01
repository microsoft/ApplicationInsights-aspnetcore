namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// This initializer sets the Authenticated User Id if enabled via <see cref="ApplicationInsightsServiceOptions.EnableAuthenticationTracking"/>.
    /// </summary>
    public class AuthenticatedUserIdTelemetryInitializer : TelemetryInitializerBase
    {
        private readonly bool enabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticatedUserIdTelemetryInitializer" /> class.
        /// </summary>
        /// <param name="options">Provides the option <see cref="ApplicationInsightsServiceOptions.EnableAuthenticationTracking"/> indicating whether the currently authenticated user is added to telemetries or not.</param>
        /// <param name="httpContextAccessor">Accessor to provide HttpContext corresponding to telemetry items.</param>
        public AuthenticatedUserIdTelemetryInitializer(IOptions<ApplicationInsightsServiceOptions> options, IHttpContextAccessor httpContextAccessor)
             : base(httpContextAccessor)
        {
            this.enabled = options.Value.EnableAuthenticationTracking;
        }

        /// <inheritdoc />
        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (this.enabled)
            {
                if (string.IsNullOrEmpty(telemetry.Context.User.AuthenticatedUserId))
                {
                    var userIdentity = platformContext.User?.Identity;

                    if (userIdentity != null &&
                        userIdentity.IsAuthenticated)                    
                    {
                        telemetry.Context.User.AuthenticatedUserId = userIdentity.Name;
                    }
                }
            }
        }
    }
}
