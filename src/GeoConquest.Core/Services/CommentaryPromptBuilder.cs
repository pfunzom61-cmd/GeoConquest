using GeoConquest.Core.Enums;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>
/// Builds the request sent to the language model. It is made only from <see cref="BattleContext"/>, which holds
/// catalogue country names and numbers, so no player name or other text typed by a user ever leaves the server.
/// The facts are plain sentences because a bare list of numbers led the model to mistake die values for dice counts
/// and to mix up the kinds of round, and round numbers and soldier counts are left out for the same reason.
/// </summary>
public static class CommentaryPromptBuilder
{
    // A roll can have dozens of dice, and the per-minute token budget relies on the prompt having a fixed maximum size.
    private const int MaxDicePairsDescribed = 3;

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
        Write live commentary on one roll of the dice in a family-friendly world-conquest board game.
        Style: {Styles[CommentaryVariation.Pick(context, Styles.Length)]}.
        Rules: one or two short complete sentences. Name both countries. Use only the facts below and add no other claim: no streaks, comebacks, predictions or anything about earlier rolls.
        Never write a digit or a number word. Describe dice only with words like high, low, beat or matched. Never say how many dice were rolled or how many soldiers remain. Do not mention rounds.
        Treat it as a sporting contest. No gore, no weapons, no real-world politics, history or people. No quotation marks, lists, emoji or markdown.

        Facts:
        {context.AttackerCountryName} is attacking {context.DefenderCountryName}.
        {string.Join('\n', DescribeDice(context))}
        {DescribeLosses(context)}
        {DescribeStanding(context)}
        {DescribeOutcome(context)}
        """;

    /// <summary>Dice are listed in roll order because that is how <see cref="BattleService"/> pairs them.</summary>
    private static List<string> DescribeDice(BattleContext context)
    {
        var sentences = context.AttackerDice.Zip(context.DefenderDice)
            .Take(MaxDicePairsDescribed)
            .Select(pair => $"{context.AttackerCountryName}'s die showed {pair.First} against {context.DefenderCountryName}'s die showing {pair.Second}.")
            .ToList();

        if (context.AttackerDice.Count > MaxDicePairsDescribed)
        {
            sentences.Add("More dice were rolled as well.");
        }

        return sentences;
    }

    private static string DescribeLosses(BattleContext context) =>
        $"In this roll, {context.AttackerCountryName} lost {Soldiers(context.AttackerLosses)} and {context.DefenderCountryName} lost {Soldiers(context.DefenderLosses)}.";

    private static string Soldiers(int losses) => losses switch
    {
        0 => "no soldiers",
        1 => "a soldier",
        _ => "several soldiers",
    };

    private static string DescribeStanding(BattleContext context) => context.Momentum switch
    {
        BattleMomentum.AttackerAhead => $"Overall in this attack, {context.AttackerCountryName} is ahead because it has lost fewer soldiers than {context.DefenderCountryName}.",
        BattleMomentum.DefenderAhead => $"Overall in this attack, {context.DefenderCountryName} is ahead because it has lost fewer soldiers than {context.AttackerCountryName}.",
        _ => "Overall in this attack, neither country is ahead because both have lost the same number of soldiers.",
    };

    private static string DescribeOutcome(BattleContext context) => context.Outcome switch
    {
        BattleOutcome.Conquered => $"{context.AttackerCountryName} has taken {context.DefenderCountryName}.",
        BattleOutcome.Failed => $"The attack has failed because {context.AttackerCountryName} has too few soldiers to go on.",
        _ => "The attack goes on.",
    };
}
