namespace GeoConquest.Web.Extensions;

/// <summary>How many sign-in and registration submissions one client address may make per time window.</summary>
public sealed class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:Auth";

    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;
}
