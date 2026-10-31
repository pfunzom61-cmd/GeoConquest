using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Web.BackgroundServices;
using Microsoft.Extensions.Options;

namespace GeoConquest.Web.Extensions;

/// <summary>Registers what plays the NPC turns in the background: the queue, the processor and the hosted service.</summary>
public static class NpcRunnerServiceCollectionExtensions
{
    public static IServiceCollection AddNpcTurnRunner(this IServiceCollection services)
    {
        services.AddOptions<NpcPacingOptions>().BindConfiguration(NpcPacingOptions.SectionName);
        // Core cannot depend on IOptions, so it receives the bound values themselves.
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<NpcPacingOptions>>().Value);

        services.AddSingleton<NpcTurnQueue>();
        services.AddSingleton<INpcTurnQueue>(provider => provider.GetRequiredService<NpcTurnQueue>());
        services.AddSingleton<INpcTurnProcessor, NpcTurnProcessor>();
        services.AddHostedService<NpcTurnBackgroundService>();

        return services;
    }
}
