using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>Chooses between wordings from the facts of a round, so commentary varies without any randomness to test around.</summary>
internal static class CommentaryVariation
{
    public static int Pick(BattleContext context, int optionCount) =>
        (context.BattleRoundNumber + context.GameRoundNumber + context.AttackerDice.Sum() + context.DefenderDice.Sum()) % optionCount;
}
