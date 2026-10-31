using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Tests.Fakes;

/// <summary>A strategy that never attacks or transfers, so tests of other behaviour are not at the mercy of NPC play.</summary>
public sealed class PassiveNpcStrategy : INpcStrategy
{
    public ReinforcementDecision DecideReinforcements(Game game, Player npc) => new(
        new Dictionary<string, int> { [game.TerritoriesOwnedBy(npc.Id)[0].CountryCode] = game.PendingReinforcements },
        $"Placing {game.PendingReinforcements} reinforcements on the first country.");

    public TransferDecision? DecideTransferBeforeAttack(Game game, Player npc) => null;

    public AttackDecision DecideAttack(Game game, Player npc) => new(null, null, "Holding: the test NPC never attacks (0 attacks).");

    public ContinueAttackDecision DecideContinueAttack(Game game, Player npc) => new(false, "Calling off the attack (0 rounds).");

    public TransferDecision? DecideTransferAfterConquest(Game game, Player npc, string attackerCountryCode, string conqueredCountryCode) => null;
}
