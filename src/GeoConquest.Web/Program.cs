using GeoConquest.Infrastructure;
using GeoConquest.Infrastructure.Identity;
using GeoConquest.Web.Extensions;
using GeoConquest.Web.Hubs;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Every state-changing request must carry an anti-forgery token, without having to remember it per action.
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddGeoConquestSecurity(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddGeoConquestIdentity().AddMongoIdentityStores();
builder.Services.AddGeoConquestAuthorization();
builder.Services.AddCoreServices();
builder.Services.AddGeoConquestRealtime();
builder.Services.AddNpcTurnRunner();
builder.Services.AddBattleCommentaryRunner();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// An empty 403, 404 or 429 gets a friendly page; JSON answers that carry a body (the API's error messages) are left alone.
app.UseStatusCodePagesWithReExecute("/status/{0}");

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHub<GameHub>(RealtimeServiceCollectionExtensions.GameHubPath);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

/// <summary>Exposed so integration tests can bootstrap the app with WebApplicationFactory.</summary>
public partial class Program;
