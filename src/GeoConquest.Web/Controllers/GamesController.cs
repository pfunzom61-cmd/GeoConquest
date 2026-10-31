using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Infrastructure.Identity;
using GeoConquest.Web.Authorization;
using GeoConquest.Web.ViewModels;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GeoConquest.Web.Controllers;

/// <summary>The pages of a campaign: the owner's list, starting a new one, and the board anyone with the link can watch.</summary>
[Route("games")]
public sealed class GamesController(
    IGameQueryService gameQueryService,
    IGameApplicationService gameApplicationService,
    ICountryRepository countryRepository,
    ISoldierAllocator soldierAllocator,
    IAuthorizationService authorizationService,
    IAntiforgery antiforgery,
    INpcTurnQueue npcTurnQueue,
    UserManager<ApplicationUser> userManager) : Controller
{
    private const int RecentEventCount = 100;

    [HttpGet("")]
    [Authorize]
    public async Task<IActionResult> Index(
        string? search, GameStatus? status, CampaignSort sort, CancellationToken cancellationToken)
    {
        var games = await gameQueryService.ListCampaignsAsync(userManager.GetUserId(User)!, search, status, sort, cancellationToken);
        return View(new CampaignListViewModel(search, status, sort, games.Select(CampaignListItemViewModel.From).ToList()));
    }

    [HttpGet("new")]
    [Authorize]
    public async Task<IActionResult> New(CancellationToken cancellationToken) =>
        View(await BuildNewCampaignAsync(null, cancellationToken));

    [HttpPost("new")]
    [Authorize]
    public async Task<IActionResult> Create(NewCampaignViewModel model, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var game = await gameApplicationService.CreateGameAsync(userManager.GetUserId(User)!, user.DisplayName, model.SelectedCountryCode!, cancellationToken);
                return RedirectToAction(nameof(Play), new { id = game.Id });
            }
            catch (GameRuleException exception)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
            }
        }

        return View(nameof(New), await BuildNewCampaignAsync(model.SelectedCountryCode, cancellationToken));
    }

    /// <summary>The board. Open to everyone: visitors watch read-only, only the owner gets the action panel.</summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Play(string id, CancellationToken cancellationToken)
    {
        var game = await gameQueryService.GetGameAsync(id, cancellationToken);
        if (game is null)
        {
            return NotFound();
        }

        // Self-heal: opening a game whose NPC turns stalled (for example after repeated failures) gets it moving again.
        // The queue ignores a game that is already waiting or being played.
        if (game.AwaitsNpcTurn)
        {
            npcTurnQueue.Enqueue(game.Id);
        }

        var viewerIsOwner = (await authorizationService.AuthorizeAsync(User, game, GamePolicies.CanPlayGame)).Succeeded;
        var recentEvents = await gameQueryService.GetRecentEventsAsync(id, RecentEventCount, cancellationToken);

        if (viewerIsOwner)
        {
            // The board script sends this with each action; the cookie half was set by this call.
            ViewData["RequestToken"] = antiforgery.GetAndStoreTokens(HttpContext).RequestToken;
        }

        return View(new BoardPageViewModel(
            GameStateViewModel.From(game, viewerIsOwner),
            recentEvents.Select(GameEventViewModel.From).ToList()));
    }

    private async Task<NewCampaignViewModel> BuildNewCampaignAsync(string? selectedCountryCode, CancellationToken cancellationToken)
    {
        var countries = await countryRepository.GetAllAsync(cancellationToken);
        return new NewCampaignViewModel
        {
            SelectedCountryCode = selectedCountryCode,
            Countries = countries
                .Select(country => new CountryOptionViewModel(
                    country.IsoCode, country.Name, country.Population, soldierAllocator.GetSoldierValue(country.Population)))
                .ToList(),
        };
    }
}
