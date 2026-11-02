using GeoConquest.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace GeoConquest.Web.Extensions;

/// <summary>Configures who can sign in and how: password policy, lockout and the authentication cookie.</summary>
public static class IdentityServiceCollectionExtensions
{
    private const int MaxFailedSignInAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(7);

    public static IdentityBuilder AddGeoConquestIdentity(this IServiceCollection services)
    {
        var builder = services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;

            options.Lockout.MaxFailedAccessAttempts = MaxFailedSignInAttempts;
            options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = CookieLifetime;
            options.SlidingExpiration = true;
            options.Cookie.Name = "GeoConquest.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        return builder;
    }
}
