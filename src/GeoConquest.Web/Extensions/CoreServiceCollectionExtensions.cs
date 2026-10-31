using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Services;

namespace GeoConquest.Web.Extensions;

/// <summary>Registers the game rules and use cases from Core. Core itself has no package references, so the wiring lives here.</summary>
public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ISoldierAllocator, SoldierAllocator>();
        services.AddSingleton<IDiceRoller, SecureDiceRoller>();
        services.AddSingleton<IShuffler, SecureShuffler>();
        services.AddSingleton<IBattleService, BattleService>();
        services.AddSingleton<IBattleOddsCalculator, BattleOddsCalculator>();
        services.AddSingleton<IReinforcementService, ReinforcementService>();
        services.AddSingleton<IGameSetupService, GameSetupService>();
        services.AddSingleton<IGameEngine, GameEngine>();
        services.AddSingleton<INpcStrategy, StrategicNpcStrategy>();
        services.AddSingleton<INpcTurnRunner, NpcTurnExecutor>();

        services.AddSingleton<BattleCommentaryScheduler>();
        services.AddSingleton<ICommentaryProcessor, BattleCommentaryProcessor>();
        services.AddSingleton<IGameProgressRecorder, GameProgressRecorder>();

        services.AddScoped<IGameApplicationService, GameApplicationService>();
        services.AddScoped<IGameQueryService, GameQueryService>();

        return services;
    }
}
