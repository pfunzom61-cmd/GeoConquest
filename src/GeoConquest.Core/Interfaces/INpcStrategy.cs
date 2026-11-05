using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>
/// Decides what an NPC does, one question at a time. A strategy only reads the game and never changes it; the turn
/// executor carries the decisions out through the game engine, so NPCs obey exactly the rules the human does.
/// Every decision carries a one-line justification that quotes the numbers behind it.
/// </summary>
public interface INpcStrategy
{
    /// <summary>Splits the NPC's pending reinforcements between its countries.</summary>
    ReinforcementDecision DecideReinforcements(Game game, Player npc);

    /// <summary>A transfer that makes an attack worthwhile when none is possible yet, or null when none is needed.</summary>
    TransferDecision? DecideTransferBeforeAttack(Game game, Player npc);

    /// <summary>Picks the turn's attack, or holds when no attack is good enough.</summary>
    AttackDecision DecideAttack(Game game, Player npc);

    /// <summary>After a round of the attack in progress, decides whether to roll again.</summary>
    ContinueAttackDecision DecideContinueAttack(Game game, Player npc);

    /// <summary>After a conquest, a transfer that evens out the attacking and conquered countries, or null when they are already close.</summary>
    TransferDecision? DecideTransferAfterConquest(Game game, Player npc, string attackerCountryCode, string conqueredCountryCode);
}
