namespace GeoConquest.Web.ViewModels;

/// <summary>What the friendly error pages say: what happened, and the one thing to do next.</summary>
public sealed record StatusPageViewModel(int Code, string Title, string Message, string? RequestId)
{
    public static StatusPageViewModel For(int code, string? requestId = null) => code switch
    {
        403 => new(code, "You cannot open that", "This page belongs to another player, or needs an account you are not signed in to. Go back to the games you can open.", requestId),
        404 => new(code, "Page not found", "That address does not match any page or game. The link may be old, or the game may have been removed.", requestId),
        429 => new(code, "Too many attempts", "You sent too many requests in a short time. Wait a minute, then try again.", requestId),
        >= 500 => new(code, "Something broke on our side", "The server hit an unexpected problem and your last step may not have been saved. Reload the page to check, then try again.", requestId),
        _ => new(code, "Something went wrong", "That request could not be completed. Go back and try again.", requestId),
    };
}
