using GeoConquest.Core.Interfaces;
using GeoConquest.Web.Hubs;

namespace GeoConquest.Web.Extensions;

/// <summary>Registers the live updates: the SignalR hub and the publisher that feeds it.</summary>
public static class RealtimeServiceCollectionExtensions
{
    /// <summary>The address the board script connects to.</summary>
    public const string GameHubPath = "/hubs/game";

    public static IServiceCollection AddGeoConquestRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IGameUpdatePublisher, SignalRGameUpdatePublisher>();
        return services;
    }
}
