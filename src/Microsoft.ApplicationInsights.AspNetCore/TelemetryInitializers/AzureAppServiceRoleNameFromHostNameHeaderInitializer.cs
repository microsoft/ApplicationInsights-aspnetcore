﻿namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.AspNetCore.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// A telemetry initializer that will gather Azure Web App Role Environment context information to
    /// populate TelemetryContext.Cloud.RoleName
    /// This uses the http header "WAS-DEFAULT-HOSTNAME" to update role name, if available.
    /// Otherwise role name is populated from "WEBSITE_HOSTNAME" environment variable.
    /// </summary>
    /// <remarks>
    /// The RoleName is expected to contain the host name + slot name, but will be same across all instances of
    /// a single App Service.
    /// Populating RoleName from HOSTNAME environment variable will cause RoleName to be incorrect when a slot swap occurs in AppService.
    /// The most accurate way to determine the RoleName is to rely on the header WAS-DEFAULT-HOSTNAME, as its
    /// populated from App service front end on every request. Slot swaps are instantly reflected in this header.
    /// </remarks>
    public class AzureAppServiceRoleNameFromHostNameHeaderInitializer : ITelemetryInitializer
    {
        private const string WebAppHostNameHeaderName = "WAS-DEFAULT-HOSTNAME";
        private const string WebAppHostNameEnvironmentVariable = "WEBSITE_HOSTNAME";
        private readonly IHttpContextAccessor httpContextAccessor;
        private string roleName;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureAppServiceRoleNameFromHostNameHeaderInitializer" /> class.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor to provide HttpContext if available.</param>
        public AzureAppServiceRoleNameFromHostNameHeaderInitializer(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            try
            {
                var result = Environment.GetEnvironmentVariable(WebAppHostNameEnvironmentVariable);

                if (!string.IsNullOrEmpty(result) && result.EndsWith(this.WebAppSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    result = result.Substring(0, result.Length - this.WebAppSuffix.Length);
                }

                this.roleName = result;
            }
            catch (Exception ex)
            {
                AspNetCoreEventSource.Instance.LogAzureAppServiceRoleNameFromHostNameHeaderInitializerWarning(ex.ToInvariantString());
            }
        }

        /// <summary>
        /// Gets or sets suffix of website name. This must be changed when running in non public Azure region.
        /// Default value (Public Cloud):  ".azurewebsites.net"
        /// For US Gov Cloud: ".azurewebsites.us"
        /// For Azure Germany: ".azurewebsites.de".
        /// </summary>
        public string WebAppSuffix { get; set; } = ".azurewebsites.net";

        /// <summary>
        /// Populates RoleName from the request telemetry associated with the http context.
        /// If RoleName is empty on the request telemetry, it'll be updated as well so that other telemetry
        /// belonging to the same requests gets it from request telemetry, without having to parse headers again.
        /// </summary>
        /// <remarks>
        /// RoleName is attempted from every incoming request as opposed to doing this periodically. This is
        /// done to ensure every request (and associated telemetry) gets the correct RoleName during slot swap.
        /// </remarks>
        /// <param name="telemetry">The telemetry item for which RoleName is to be set.</param>
        public void Initialize(ITelemetry telemetry)
        {
            try
            {
                if (!string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
                {
                    // RoleName is already populated.
                    return;
                }

                string roleName = string.Empty;
                var context = this.httpContextAccessor.HttpContext;

                if (context != null)
                {
                    lock (context)
                    {
                        var request = context.Features.Get<RequestTelemetry>();

                        if (request != null)
                        {
                            if (string.IsNullOrEmpty(request.Context.Cloud.RoleName))
                            {
                                roleName = this.GetRoleNameFromHeader(context);
                                if (!string.IsNullOrEmpty(roleName))
                                {
                                    request.Context.Cloud.RoleName = roleName;
                                }
                            }
                            else
                            {
                                roleName = request.Context.Cloud.RoleName;
                            }
                        }
                        else
                        {
                            roleName = this.GetRoleNameFromHeader(context);
                        }
                    }
                }

                if (string.IsNullOrEmpty(roleName))
                {
                    // Fallback to value from ENV variable.
                    roleName = this.roleName;
                }

                telemetry.Context.Cloud.RoleName = roleName;
            }
            catch (Exception ex)
            {
                AspNetCoreEventSource.Instance.LogAzureAppServiceRoleNameFromHostNameHeaderInitializerWarning(ex.ToInvariantString());
            }
        }

        private string GetRoleNameFromHeader(HttpContext context)
        {
            string roleName = string.Empty;
            if (context.Request?.Headers != null)
            {
                string headerValue = context.Request.Headers[WebAppHostNameHeaderName];
                if (!string.IsNullOrEmpty(headerValue))
                {
                    if (headerValue.EndsWith(this.WebAppSuffix, StringComparison.OrdinalIgnoreCase))
                    {
                        headerValue = headerValue.Substring(0, headerValue.Length - this.WebAppSuffix.Length);
                    }

                    roleName = headerValue;
                }
            }

            return roleName;
        }
    }
}
