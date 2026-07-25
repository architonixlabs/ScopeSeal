using Microsoft.Extensions.Options;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Api.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IOptions<ScopeSealOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headerOptions = options.Value.Security.Headers;
        if (headerOptions.Enabled)
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Content-Security-Policy"] = headerOptions.ContentSecurityPolicy;

            if (headerOptions.StrictTransportSecurityMaxAgeSeconds > 0 && context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] =
                    $"max-age={headerOptions.StrictTransportSecurityMaxAgeSeconds}; includeSubDomains";
            }

            headers.Remove("Server");
        }

        await next(context);
    }
}
