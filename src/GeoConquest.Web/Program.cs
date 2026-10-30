using GeoConquest.Infrastructure;
using GeoConquest.Infrastructure.Identity;
using GeoConquest.Web.Extensions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Every state-changing request must carry an anti-forgery token, without having to remember it per action.
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddGeoConquestSecurity(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddGeoConquestIdentity().AddMongoIdentityStores();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

/// <summary>Exposed so integration tests can bootstrap the app with WebApplicationFactory.</summary>
public partial class Program;
