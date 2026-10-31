using GeoConquest.Core.Enums;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>
/// Builds the request sent to the language model. It is made only from <see cref="BattleContext"/>, which holds
/// catalogue country names and numbers, so no player name or other text typed by a user ever leaves the server.
/// </summary>
public static class CommentaryPromptBuilder
{
    private static readonly string[] Styles =
    [
        "a breathless radio commentator",
        "a dry-witted veteran analyst",
        "an excited stadium announcer",
        "a calm, amused documentary narrator",
        "a cheerful local radio host",
    ];

    public static string Build(BattleContext context) =>
        $"""
        Write live commentary for one dice round of a family-friendly world-conquest board game.
        Style: {Styles[CommentaryVariation.Pick(context, Styles.Length)]}.
        Rules: 1 or 2 complete sentences, at most 35 words in total. Mention both countries and the dice. Use only the facts below.
        Treat it as a sporting contest. No gore, no weapons, no real-world politics, history or people. No quotation marks, lists, emoji or markdown.

        Facts:
        Attacking country: {context.AttackerCountryName}
        Defending country: {context.DefenderCountryName}
        Attacking soldiers: {context.AttackerSoldiersBefore} before, {context.AttackerSoldiersAfter} after
        Defending soldiers: {context.DefenderSoldiersBefore} before, {context.DefenderSoldiersAfter} after
        Attacker dice: {string.Join(", ", context.AttackerDice)}
        Defender dice: {string.Join(", ", context.DefenderDice)}
        Attack round: {context.BattleRoundNumber}
        Game round: {context.GameRoundNumber}
        Momentum so far: {DescribeMomentum(context.Momentum)}
        Result: {DescribeOutcome(context.Outcome)}
        """;

    private static string DescribeMomentum(BattleMomentum momentum) => momentum switch
    {
        BattleMomentum.AttackerAhead => "attacker ahead",
        BattleMomentum.DefenderAhead => "defender ahead",
        _ => "even",
    };

    private static string DescribeOutcome(BattleOutcome outcome) => outcome switch
    {
        BattleOutcome.Conquered => "the attacker takes the defending country",
        BattleOutcome.Failed => "the attack fails",
        _ => "the attack goes on",
    };
}
