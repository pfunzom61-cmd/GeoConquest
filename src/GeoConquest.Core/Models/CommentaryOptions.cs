namespace GeoConquest.Core.Models;

/// <summary>How much commentary may cost in requests and time. Bound from the "Commentary" configuration section.</summary>
public sealed class CommentaryOptions
{
    public const string SectionName = "Commentary";

    /// <summary>
    /// Requests allowed to the language model per minute, across all games: the provider's limit is per account.
    /// Set from the 8,000 tokens a minute budget: 10 requests of at most about 750 tokens each is 7,500.
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 10;

    /// <summary>How long a round's commentary may take, counted from the moment its roll is saved.</summary>
    public int TimeBudgetMilliseconds { get; set; } = 2500;

    /// <summary>With less time than this left, the model cannot answer in time, so no request is spent on trying.</summary>
    public int MinimumAiBudgetMilliseconds { get; set; } = 400;

    public TimeSpan TimeBudget => TimeSpan.FromMilliseconds(TimeBudgetMilliseconds);

    public TimeSpan MinimumAiBudget => TimeSpan.FromMilliseconds(MinimumAiBudgetMilliseconds);
}
