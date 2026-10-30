using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Resolves the dice rounds of an attack.</summary>
public interface IBattleService
{
    /// <summary>
    /// Rolls one round for the game's active battle: both sides roll the same number of dice, die i is compared with
    /// die i in roll order, the higher die wins and the loser loses a soldier, ties lose nothing. If the defender is
    /// left with no soldiers the country changes hands and the conqueror's reward becomes its new garrison.
    /// The round is recorded on the battle and returned.
    /// </summary>
    /// <exception cref="Exceptions.GameRuleException">There is no attack in progress, or the attacker has fewer than 2 soldiers.</exception>
    BattleRound RollRound(Game game);
}
