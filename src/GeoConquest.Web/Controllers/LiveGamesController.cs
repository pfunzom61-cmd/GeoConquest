using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoConquest.Web.Controllers;

/// <summary>The public list of games, so anyone can pick one to watch.</summary>
[Route("live")]
[AllowAnonymous]
public sealed class LiveGamesController(IGameQueryService gameQueryService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search, LiveGameStatusFilter status, LiveGameSort sort, CancellationToken cancellationToken)
    {
        // A status or sort that does not parse binds to the first enum value (the default); the page never errors on it.
        status = Enum.IsDefined(status) ? status : default;
        sort = Enum.IsDefined(sort) ? sort : default;

        var games = await gameQueryService.ListLiveGamesAsync(search, status, sort, cancellationToken);
        return View(new LiveGamesViewModel(search, status, sort, games.Select(LiveGameListItemViewModel.From).ToList()));
    }
}
