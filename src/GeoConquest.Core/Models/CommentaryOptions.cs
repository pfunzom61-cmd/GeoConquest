namespace GeoConquest.Core.Models;

/// <summary>How much commentary may cost in requests and time. Bound from the "Commentary" configuration section.</summary>
public sealed class CommentaryOptions
{
    public const string SectionName = "Commentary";

    /// <summary>Requests allowed to the language model per minute, across all games: the provider's limit is per account.</summary>
    public int MaxRequestsPerMinute { get; set; } = 25;

    /// <summary>How long a round's commentary may take, counted from the moment its roll is saved.</summary>
    public int TimeBudgetMilliseconds { get; set; } = 2500;

    /// <summary>With less time than this left, the model cannot answer in time, so no request is spent on trying.</summary>
    public int MinimumAiBudgetMilliseconds { get; set; } = 400;

    public TimeSpan TimeBudget => TimeSpan.FromMilliseconds(TimeBudgetMilliseconds);

    public TimeSpan MinimumAiBudget => TimeSpan.FromMilliseconds(MinimumAiBudgetMilliseconds);
}
