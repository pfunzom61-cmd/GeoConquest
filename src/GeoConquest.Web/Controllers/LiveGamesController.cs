using GeoConquest.Core.Interfaces;
using GeoConquest.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoConquest.Web.Controllers;

/// <summary>The public list of games in progress, so anyone can pick one to watch.</summary>
[Route("live")]
[AllowAnonymous]
public sealed class LiveGamesController(IGameQueryService gameQueryService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        var games = await gameQueryService.ListLiveGamesAsync(search, cancellationToken);
        return View(new LiveGamesViewModel(search, games.Select(LiveGameListItemViewModel.From).ToList()));
    }
}
