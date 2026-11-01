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
    [InlineData("Brazil rolls a 4 against Chile.")]
    [InlineData("Brazil rolls four dice against Chile.")]
    [InlineData("Brazil gives zero ground to Chile.")]
    [InlineData("Ten cheers for Brazil against Chile.")]
    [InlineData("Brazil rolls sixes and a pair of fives against Chile.")] // plural number words
    [InlineData("Brazil wins twice against Chile.")] // counting words
    public void Clean_DigitOrNumberWord_IsRejected(string rawText)
    {
        Assert.Null(CommentaryOutputGuard.Clean(rawText, BattleContexts.BrazilAttacksChile));
    }

    [Fact]
    public void Clean_NumberWordInsideALongerWord_IsKept()
    {
        const string line = "Sierra Leone rolls high and someone in Liechtenstein cheers.";
        var context = BattleContexts.BrazilAttacksChile with { AttackerCountryName = "Sierra Leone", DefenderCountryName = "Liechtenstein" };

        Assert.Equal(line, CommentaryOutputGuard.Clean(line, context));
    }

    [Theory]
    [InlineData("Brazil rolls high! Chile answers well. Then a third sentence follows.", "Brazil rolls high! Chile answers well.")]
    [InlineData("Brazil rolls high over Chile. And then the model ran out of tok", "Brazil rolls high over Chile.")]
    [InlineData("Brazil rolls high over Chile. " + LongSentence, "Brazil rolls high over Chile.")]
    [InlineData("\"**Brazil** rolls high!\n\nChile   wobbles.\"", "Brazil rolls high! Chile wobbles.")]
    public void Clean_UsableOutput_BecomesATidyLineOfWholeSentencesWithinTheLimits(string rawText, string expected)
    {
        Assert.Equal(expected, CommentaryOutputGuard.Clean(rawText, BattleContexts.BrazilAttacksChile));
    }
}
