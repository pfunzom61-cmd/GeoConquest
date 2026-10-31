using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;

namespace GeoConquest.Core.Tests.Services;

public sealed class CommentaryOutputGuardTests
{
    private const string LongSentence =
        "Brazil presses on and on through a very long and winding description of the dice that keeps going without any pause " +
        "for breath at all while the crowd waits patiently for the end of this sentence which never quite seems to arrive, and then it goes on further still.";

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("***")] // nothing left once tidied
    [InlineData("A great day for everyone at the table.")] // names neither country
    [InlineData("Brazil goes in for the kill against Chile.")] // not family-friendly
    [InlineData("Brazil rolls high and Chile")] // cut off before the first sentence ended
    [InlineData(LongSentence)] // oversized with no shorter sentence to fall back on
    public void Clean_UnusableOutput_IsRejected(string? rawText)
    {
        Assert.Null(CommentaryOutputGuard.Clean(rawText, BattleContexts.BrazilAttacksChile));
    }

    [Theory]
    [InlineData("Brazil rolls a six! Chile answers with a five. Then a third sentence follows.", "Brazil rolls a six! Chile answers with a five.")]
    [InlineData("Brazil rolls a six over Chile. And then the model ran out of tok", "Brazil rolls a six over Chile.")]
    [InlineData("Brazil rolls a six over Chile. " + LongSentence, "Brazil rolls a six over Chile.")]
    [InlineData("\"**Brazil** rolls a six!\n\nChile   wobbles.\"", "Brazil rolls a six! Chile wobbles.")]
    public void Clean_UsableOutput_BecomesATidyLineOfWholeSentencesWithinTheLimits(string rawText, string expected)
    {
        Assert.Equal(expected, CommentaryOutputGuard.Clean(rawText, BattleContexts.BrazilAttacksChile));
    }
}
