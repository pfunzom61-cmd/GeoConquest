using GeoConquest.Core.Interfaces;
using GeoConquest.Web.BackgroundServices;

namespace GeoConquest.Web.Extensions;

/// <summary>Registers what writes commentary in the background: the queue and the hosted service that works through it.</summary>
public static class CommentaryServiceCollectionExtensions
{
    public static IServiceCollection AddBattleCommentaryRunner(this IServiceCollection services)
    {
        services.AddSingleton<CommentaryQueue>();
        services.AddSingleton<ICommentaryQueue>(provider => provider.GetRequiredService<CommentaryQueue>());
        services.AddHostedService<CommentaryBackgroundService>();

        return services;
    }
}
