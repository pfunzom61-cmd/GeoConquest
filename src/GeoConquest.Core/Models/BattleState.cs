namespace GeoConquest.Core.Models;

/// <summary>An attack in progress. Holds country codes (not territories) so the game stays a single flat document.</summary>
public sealed class BattleState
{
    private readonly List<BattleRound> _rounds = [];

    public BattleState(string attackerCountryCode, string defenderCountryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attackerCountryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(defenderCountryCode);

        AttackerCountryCode = attackerCountryCode;
        DefenderCountryCode = defenderCountryCode;
    }

    public string AttackerCountryCode { get; }

    public string DefenderCountryCode { get; }

    public IReadOnlyList<BattleRound> Rounds => _rounds;

    public void AddRound(BattleRound round) => _rounds.Add(round);
}
