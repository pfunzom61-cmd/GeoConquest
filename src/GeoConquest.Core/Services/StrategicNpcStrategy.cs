using GeoConquest.Core.Constants;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>
/// The single strategy every NPC uses. It plays to win by weighing exact battle odds:
/// reinforce threatened countries and the strongest attack base, attack the target with the best odds-weighted
/// reward, call the attack off when the odds collapse, and even out soldiers after a conquest.
/// All thresholds live in <see cref="NpcTuning"/>.
/// </summary>
public sealed class StrategicNpcStrategy(IBattleOddsCalculator oddsCalculator) : INpcStrategy
{
    private readonly IAttackOddsEstimator oddsEstimator = new CappedAttackOddsEstimator(oddsCalculator);

    /// <summary>One attack the NPC could launch, with the odds and the score used to compare it with the others.</summary>
    private sealed record AttackOption(Territory Attacker, int AttackerSoldiers, Territory Defender, double Probability, double Score);

    public ReinforcementDecision DecideReinforcements(Game game, Player npc)
    {
        var ownedTerritories = game.TerritoriesOwnedBy(npc.Id);
        var strongestEnemyStack = EnemyTerritories(game, npc).Select(territory => territory.Soldiers).DefaultIfEmpty(0).Max();

        var allocations = new Dictionary<string, int>();
        var reasons = new Dictionary<string, List<string>>(); // country code -> why soldiers went there, in order
        var defenceBudget = (int)(game.PendingReinforcements * NpcTuning.DefenceShare);

        // Most valuable countries first, so the budget goes where a loss would cost the most.
        foreach (var territory in ownedTerritories.OrderByDescending(owned => owned.SoldierValue))
        {
            var oddsBefore = WinProbability(strongestEnemyStack, territory.Soldiers);
            if (defenceBudget == 0 || oddsBefore < NpcTuning.ThreatThreshold)
            {
                continue;
            }

            var needed = 1;
            while (needed <= defenceBudget && WinProbability(strongestEnemyStack, territory.Soldiers + needed) >= NpcTuning.ThreatThreshold)
            {
                needed++;
            }

            if (needed > defenceBudget)
            {
                continue; // Cannot be saved with the soldiers available, so don't waste them.
            }

            var oddsAfter = WinProbability(strongestEnemyStack, territory.Soldiers + needed);
            allocations[territory.CountryCode] = needed;
            defenceBudget -= needed;
            reasons[territory.CountryCode] = [$"{Wording.Count(needed, "soldier")} because the enemy stack of {strongestEnemyStack} would take it " +
                $"{Percent(oddsBefore)} of the time, now {Percent(oddsAfter)}"];
        }

        var remaining = game.PendingReinforcements - allocations.Values.Sum();
        if (remaining > 0)
        {
            var attackBase = ownedTerritories.OrderByDescending(owned => owned.Soldiers).First();
            allocations[attackBase.CountryCode] = allocations.GetValueOrDefault(attackBase.CountryCode) + remaining;
            if (!reasons.TryGetValue(attackBase.CountryCode, out var baseReasons))
            {
                reasons[attackBase.CountryCode] = baseReasons = [];
            }

            baseReasons.Add($"{Wording.Count(remaining, "soldier")} because it is the strongest base ({Wording.Count(attackBase.Soldiers, "soldier")})");
        }

        // One entry per country: "2 in Gibraltar (1 soldier because ...; 1 soldier because ...)", never the same country twice.
        var placements = allocations.Select(allocation =>
        {
            var country = game.GetTerritory(allocation.Key).CountryName;
            var why = reasons[allocation.Key];
            return why.Count == 1 ? $"{allocation.Value} in {country} ({why[0]})" : $"{allocation.Value} in {country} ({string.Join("; ", why)})";
        });

        return new ReinforcementDecision(
            allocations, $"Placing {Wording.Count(game.PendingReinforcements, "reinforcement")}: {string.Join("; ", placements)}.");
    }

    public TransferDecision? DecideTransferBeforeAttack(Game game, Player npc)
    {
        var ownedTerritories = game.TerritoriesOwnedBy(npc.Id);
        var leader = Leader(game);

        if (ownedTerritories.Any(territory => BestAttackFrom(game, npc, leader, territory, territory.Soldiers) is not null))
        {
            return null;
        }

        // No attack is good enough yet: look for a country whose surplus, moved into another, unlocks one.
        var unlocking = (
            from donor in ownedTerritories
            where donor.Soldiers >= GameRules.MinimumAttackingSoldiers
            from receiver in ownedTerritories
            where receiver != donor
            let surplus = donor.Soldiers - 1
            let option = BestAttackFrom(game, npc, leader, receiver, receiver.Soldiers + surplus)
            where option is not null
            select new { donor, receiver, surplus, option })
            .OrderByDescending(candidate => candidate.option!.Score)
            .FirstOrDefault();

        if (unlocking is null)
        {
            return null;
        }

        var (donorTerritory, receiverTerritory, soldiers) = (unlocking.donor, unlocking.receiver, unlocking.surplus);
        return new TransferDecision(
            donorTerritory.CountryCode,
            receiverTerritory.CountryCode,
            soldiers,
            $"Moving {Wording.Count(soldiers, "soldier")} from {donorTerritory.CountryName} to {receiverTerritory.CountryName} " +
            $"({receiverTerritory.Soldiers} to {receiverTerritory.Soldiers + soldiers}) to open an attack on " +
            $"{unlocking.option!.Defender.CountryName} at {Percent(unlocking.option.Probability)} win chance.");
    }

    public AttackDecision DecideAttack(Game game, Player npc)
    {
        var leader = Leader(game);
        var attackers = game.TerritoriesOwnedBy(npc.Id).Where(territory => territory.Soldiers >= GameRules.MinimumAttackingSoldiers).ToList();

        var best = attackers
            .Select(attacker => BestAttackFrom(game, npc, leader, attacker, attacker.Soldiers))
            .OfType<AttackOption>()
            .MaxBy(option => option.Score);

        return best is null ? Hold(game, npc, attackers) : Attack(game, leader, best);
    }

    public ContinueAttackDecision DecideContinueAttack(Game game, Player npc)
    {
        var battle = game.ActiveBattle ?? throw new GameRuleException("There is no attack in progress.");
        var attackingTerritory = game.GetTerritory(battle.AttackerCountryCode);
        var defendingTerritory = game.GetTerritory(battle.DefenderCountryCode);
        var odds = WinProbability(attackingTerritory.Soldiers, defendingTerritory.Soldiers);
        var matchup = $"{defendingTerritory.CountryName} ({attackingTerritory.Soldiers} vs {defendingTerritory.Soldiers})";

        return odds < NpcTuning.StopThreshold
            ? new ContinueAttackDecision(false, $"Calling off the attack on {matchup}: {Percent(odds)} win chance, below the {Percent(NpcTuning.StopThreshold)} floor.")
            : new ContinueAttackDecision(true, $"Pressing on against {matchup}: {Percent(odds)} win chance.");
    }

    public TransferDecision? DecideTransferAfterConquest(
        Game game, Player npc, string attackerCountryCode, string conqueredCountryCode)
    {
        var attackingTerritory = game.GetTerritory(attackerCountryCode);
        var conqueredTerritory = game.GetTerritory(conqueredCountryCode);

        // Moving half the difference leaves both countries about level; the attacker always keeps at least 1.
        var soldiers = (attackingTerritory.Soldiers - conqueredTerritory.Soldiers) / 2;
        if (soldiers < 1)
        {
            return null;
        }

        return new TransferDecision(
            attackerCountryCode,
            conqueredCountryCode,
            soldiers,
            $"Moving {Wording.Count(soldiers, "soldier")} from {attackingTerritory.CountryName} to {conqueredTerritory.CountryName} to balance them " +
            $"({attackingTerritory.Soldiers} vs {conqueredTerritory.Soldiers} becomes " +
            $"{attackingTerritory.Soldiers - soldiers} vs {conqueredTerritory.Soldiers + soldiers}).");
    }

    private AttackDecision Hold(Game game, Player npc, List<Territory> attackers)
    {
        var targets = LegalTargets(game, npc);

        if (attackers.Count == 0 || targets.Count == 0)
        {
            return new AttackDecision(null, null,
                $"Holding: no attack is possible ({attackers.Count} countries with {GameRules.MinimumAttackingSoldiers}+ soldiers, {targets.Count} legal targets).");
        }

        var (attacker, defender, odds) = (
            from candidateAttacker in attackers
            from candidateDefender in targets
            select (candidateAttacker, candidateDefender, WinProbability(candidateAttacker.Soldiers, candidateDefender.Soldiers)))
            .MaxBy(pairing => pairing.Item3);

        return new AttackDecision(null, null,
            $"Holding: best odds are {Percent(odds)} ({attacker.CountryName} {attacker.Soldiers} vs {defender.CountryName} {defender.Soldiers}), " +
            $"below the {Percent(NpcTuning.AttackThreshold)} needed.");
    }

    private static AttackDecision Attack(Game game, Player leader, AttackOption option)
    {
        var owner = game.GetPlayer(option.Defender.OwnerPlayerId);
        var justification = $"Attacking {Wording.HeldCountry(option.Defender, owner)} from {option.Attacker.CountryName} " +
            $"({option.AttackerSoldiers} vs {option.Defender.Soldiers}): {Percent(option.Probability)} win chance, " +
            $"+{GameRules.ConquestReward(option.Defender.SoldierValue)} soldiers";

        if (game.TerritoriesOwnedBy(owner.Id).Count == 1)
        {
            justification += $", eliminating {Wording.PlayerName(owner)}";
        }
        else if (owner.Id == leader.Id)
        {
            justification += $", weakening the leader {Wording.PlayerName(owner)}";
        }

        return new AttackDecision(option.Attacker.CountryCode, option.Defender.CountryCode, justification + ".");
    }

    /// <summary>The best attack from one country, given a soldier count that may be hypothetical (after a transfer), or null if none reaches the threshold.</summary>
    private AttackOption? BestAttackFrom(Game game, Player npc, Player leader, Territory attacker, int attackerSoldiers)
    {
        if (attackerSoldiers < GameRules.MinimumAttackingSoldiers)
        {
            return null;
        }

        return LegalTargets(game, npc)
            .Select(defender =>
            {
                var odds = WinProbability(attackerSoldiers, defender.Soldiers);
                return new AttackOption(attacker, attackerSoldiers, defender, odds, Score(game, leader, defender, odds));
            })
            .Where(option => option.Probability >= NpcTuning.AttackThreshold)
            .MaxBy(option => option.Score);
    }

    // Spec: reward = Max(2, value / 2); the bonuses favour weakening the leader and finishing off a player.
    private static double Score(Game game, Player leader, Territory defender, double odds)
    {
        var ownerId = defender.OwnerPlayerId;
        var bonus = (ownerId == leader.Id ? NpcTuning.LeaderBonus : 0)
            + (game.TerritoriesOwnedBy(ownerId).Count == 1 ? NpcTuning.EliminationBonus : 0);

        return odds * (GameRules.ConquestReward(defender.SoldierValue) + bonus);
    }

    private static List<Territory> EnemyTerritories(Game game, Player npc) =>
        game.Territories.Where(territory => territory.OwnerPlayerId != npc.Id).ToList();

    /// <summary>Enemy countries the NPC may attack. The human is protected in the opening round (assessment assumption 11).</summary>
    private static List<Territory> LegalTargets(Game game, Player npc) =>
        EnemyTerritories(game, npc)
            .Where(territory => game.RoundNumber > NpcTuning.HumanProtectedThroughRound || !game.GetPlayer(territory.OwnerPlayerId).IsHuman)
            .ToList();

    /// <summary>The living player with the most countries, then the most soldiers.</summary>
    private static Player Leader(Game game) =>
        game.Players
            .Where(player => !player.IsEliminated)
            .OrderByDescending(player => game.TerritoriesOwnedBy(player.Id).Count)
            .ThenByDescending(player => game.TerritoriesOwnedBy(player.Id).Sum(territory => territory.Soldiers))
            .First();

    private double WinProbability(int attackerSoldiers, int defenderSoldiers) =>
        oddsEstimator.Estimate(attackerSoldiers, defenderSoldiers);

    private static string Percent(double probability) => $"{(int)Math.Round(probability * 100)}%";
}
