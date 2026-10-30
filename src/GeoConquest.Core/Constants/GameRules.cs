namespace GeoConquest.Core.Constants;

/// <summary>Numeric rules from the game specification, shared by the battle, odds and reinforcement code.</summary>
public static class GameRules
{
    /// <summary>The human's country plus nine NPC countries.</summary>
    public const int CountriesPerGame = 10;

    /// <summary>A country needs at least this many soldiers to start or continue an attack.</summary>
    public const int MinimumAttackingSoldiers = 2;

    public const int MinimumConquestReward = 2;

    public const int ReinforcementsPerCountry = 2;

    public const int DieFaces = 6;

    /// <summary>
    /// Dice each side rolls: the smaller of the attacker's soldiers minus the one that must stay behind,
    /// and the defender's soldiers. Examples: 10v2 gives 2, 3v5 gives 2, 2v5 gives 1.
    /// </summary>
    public static int DicePerSide(int attackerSoldiers, int defenderSoldiers) =>
        Math.Min(attackerSoldiers - 1, defenderSoldiers);

    /// <summary>Spec: reward = Max(2, conqueredCountrySoldierValue / 2), using integer division.</summary>
    public static int ConquestReward(int conqueredSoldierValue) =>
        Math.Max(MinimumConquestReward, conqueredSoldierValue / 2);
}
