using System.Text.RegularExpressions;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>
/// Decides whether text from the language model may be shown. A model's output is untrusted: it can be empty, cut off,
/// far too long, about something else, or unsuitable for a family audience. Anything that cannot be repaired is rejected,
/// and the caller then uses a template instead.
/// </summary>
public static partial class CommentaryOutputGuard
{
    public const int MaxLength = 240;
    private const int MaxSentences = 2;

    /// <summary>Returns a tidy single line of whole sentences, or null if the text must not be shown.</summary>
    public static string? Clean(string? rawText, BattleContext context)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var tidied = Whitespace().Replace(rawText.Replace("*", "").Replace("`", "").Replace("#", ""), " ").Trim(' ', '"', '\'', '“', '”', '‘', '’');
        var text = KeepWholeSentencesWithinLimits(tidied);

        var isAboutThisBattle = text.Contains(context.AttackerCountryName, StringComparison.OrdinalIgnoreCase)
            || text.Contains(context.DefenderCountryName, StringComparison.OrdinalIgnoreCase);

        return isAboutThisBattle && !UnsuitableWord().IsMatch(text) ? text : null;
    }

    /// <summary>
    /// Text without closing punctuation was cut off mid-sentence, so that part is dropped. Sentences are then removed
    /// from the end until the limits are met, which leaves nothing if the first sentence alone is too long.
    /// </summary>
    private static string KeepWholeSentencesWithinLimits(string text)
    {
        var sentences = SentenceBoundary().Split(text).Where(sentence => sentence is [.., '.' or '!' or '?']).Take(MaxSentences).ToList();

        while (sentences.Count > 0 && string.Join(' ', sentences).Length > MaxLength)
        {
            sentences.RemoveAt(sentences.Count - 1);
        }

        return string.Join(' ', sentences);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceBoundary();

    // "die" and "dead" are left out on purpose: a commentator talks about a die, and a "dead heat", in every game.
    [GeneratedRegex(
        @"\b(kill\w*|blood\w*|slaughter\w*|massacre\w*|murder\w*|corpse\w*|gore|gory|genocide|rape\w*|nazi\w*|terroris\w*|behead\w*|torture\w*|fuck\w*|shit\w*|bitch\w*|bastard\w*|damn\w*|hell|ass|asshole\w*)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex UnsuitableWord();
}
