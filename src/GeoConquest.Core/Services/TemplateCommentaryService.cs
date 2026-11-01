using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>
/// Commentary with no outside dependency, used whenever the model cannot answer. The wording is picked from the facts
/// rather than at random, so the same round always reads the same while different rounds still read differently.
/// </summary>
public sealed class TemplateCommentaryService : ICommentaryService
{
    public Task<GeneratedCommentary> DescribeRoundAsync(BattleContext context, TimeSpan timeBudget, CancellationToken cancellationToken)
    {
        var lines = LinesFor(context);
        var line = lines[CommentaryVariation.Pick(context, lines.Count)];
        return Task.FromResult(new GeneratedCommentary(line, CommentarySource.Template));
    }

    private static List<string> LinesFor(BattleContext context)
    {
        var attacker = context.AttackerCountryName;
        var defender = context.DefenderCountryName;
        var attackerDice = string.Join(", ", context.AttackerDice);
        var defenderDice = string.Join(", ", context.DefenderDice);
        var attackerLeft = Wording.Count(context.AttackerSoldiersAfter, "soldier");
        var defenderLeft = Wording.Count(context.DefenderSoldiersAfter, "soldier");

        return (context.Outcome, context.Momentum) switch
        {
            (BattleOutcome.Conquered, _) =>
            [
                $"{defender} has no one left to defend it! {attacker} rolls {attackerDice} against {defenderDice} and takes the country.",
                $"It's all over in {defender}: {attacker}'s {attackerDice} beats {defenderDice} and the flag changes hands.",
                $"What a finish! {attacker} throws {attackerDice} to {defenderDice} and claims {defender} in round {context.BattleRoundNumber} of the attack.",
            ],
            (BattleOutcome.Failed, _) =>
            [
                $"{defender} holds firm! {defenderDice} turns back {attacker}'s {attackerDice}, and the attack has run out of steam.",
                $"The charge fades: {attacker} rolls {attackerDice}, {defender} answers {defenderDice}, and only {attackerLeft} is left to head home.",
                $"A superb defence by {defender}: {defenderDice} against {attackerDice} ends {attacker}'s attack.",
            ],
            (_, BattleMomentum.AttackerAhead) =>
            [
                $"{attacker} keeps up the pressure: {attackerDice} against {defenderDice} leaves {defender} with {defenderLeft}.",
                $"Momentum with {attacker}! The dice read {attackerDice} to {defenderDice}, and {defender} is down to {defenderLeft}.",
            ],
            (_, BattleMomentum.DefenderAhead) =>
            [
                $"{defender} is making this hard work: {defenderDice} against {attackerDice} leaves {attacker} with {attackerLeft}.",
                $"Stubborn stuff from {defender}! {attacker} rolls {attackerDice}, meets {defenderDice}, and has {attackerLeft} left.",
            ],
            _ =>
            [
                $"Nothing between them in round {context.BattleRoundNumber}: {attacker} rolls {attackerDice}, {defender} rolls {defenderDice}, with {attackerLeft} and {defenderLeft} left.",
                $"Neck and neck! {attacker}'s {attackerDice} meets {defender}'s {defenderDice}; it's {attackerLeft} against {defenderLeft}.",
            ],
        };
    }
}
