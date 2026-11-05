namespace GeoConquest.Web.ViewModels;

/// <summary>
/// What a dossier card in a list shows: the same card as the board, filled with a campaign or a live game.
/// <paramref name="Stamp"/> is a red-ink mark such as "Won", or null for none.
/// </summary>
public sealed record ListCardViewModel(
    string Tab,
    string CountryCode,
    string Title,
    IReadOnlyList<string> Details,
    string? Stamp,
    string FootLabel,
    string FootValue,
    string LinkText,
    string LinkHiddenText,
    string LinkUrl);
