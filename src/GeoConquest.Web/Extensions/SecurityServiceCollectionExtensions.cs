using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace GeoConquest.Web.Extensions;

/// <summary>Request-level protection: throttling of the sign-in forms and correct client details behind the host's proxy.</summary>
public static class SecurityServiceCollectionExtensions
{
    /// <summary>Name of the rate-limit policy applied to the login and register submissions.</summary>
    public const string AuthRateLimitPolicy = "auth";

    public static IServiceCollection AddGeoConquestSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthRateLimitOptions>(configuration.GetSection(AuthRateLimitOptions.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, cancellationToken) =>
                new ValueTask(context.HttpContext.Response.WriteAsync("Too many attempts. Please wait a minute and try again.", cancellationToken));

            options.AddPolicy(AuthRateLimitPolicy, httpContext =>
            {
                var limit = httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit.PermitLimit,
                        Window = TimeSpan.FromSeconds(limit.WindowSeconds),
                    });
            });
        });

        // The host terminates TLS at its proxy. Without this every visitor appears to share the proxy's address,
        // so one person could use up everyone's rate limit, and requests would not be recognised as HTTPS.
        // ForwardLimit is how many proxy hops to trust, counted from the end of X-Forwarded-For. 1 is safe against
        // spoofed headers; raise it (ForwardedHeaders__ForwardLimit) only if the host puts a second proxy in front.
        var forwardLimit = configuration.GetValue("ForwardedHeaders:ForwardLimit", 1);
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardLimit = forwardLimit;
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }
}
