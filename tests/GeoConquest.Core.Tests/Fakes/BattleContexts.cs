using GeoConquest.Core.Enums;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Tests.Fakes;

/// <summary>A typical dice round, for tests to adjust with a <c>with</c> expression.</summary>
public static class BattleContexts
{
    public static readonly BattleContext BrazilAttacksChile = new(
        "Brazil", "Chile", 8, 7, 3, 2, [6, 2], [5, 3], 1, 1, 3, 2, 2, 4, BattleOutcome.Continuing);
}
