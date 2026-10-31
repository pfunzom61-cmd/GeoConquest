using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Web.Authorization;
using GeoConquest.Web.Filters;
using GeoConquest.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoConquest.Web.Controllers;

/// <summary>
/// The JSON endpoints the board posts to. The browser only sends intents; the engine decides what is allowed and
/// rolls the dice. Every action first checks the caller owns the game (401 if signed out, 403 if someone else's).
/// </summary>
[Route("api/games/{gameId}")]
[AllowAnonymous]
[GameErrorFilter]
public sealed class GameActionsController(
    IGameQueryService gameQueryService,
    IGameApplicationService gameApplicationService,
    IAuthorizationService authorizationService,
    INpcTurnQueue npcTurnQueue) : Controller
{
    [HttpPost("allocate")]
    public Task<IActionResult> Allocate(string gameId, [FromBody] AllocateReinforcementsRequest request, CancellationToken cancellationToken) =>
        PerformAsync(gameId, game =>
            gameApplicationService.AllocateReinforcementsAsync(game, request.Allocations!, cancellationToken), cancellationToken);

    [HttpPost("transfer")]
    public Task<IActionResult> Transfer(string gameId, [FromBody] TransferRequest request, CancellationToken cancellationToken) =>
        PerformAsync(gameId, game =>
            gameApplicationService.TransferAsync(game, request.FromCountryCode!, request.ToCountryCode!, request.Soldiers, cancellationToken), cancellationToken);

    [HttpPost("attack")]
    public Task<IActionResult> Attack(string gameId, [FromBody] AttackRequest request, CancellationToken cancellationToken) =>
        PerformAsync(gameId, game =>
            gameApplicationService.StartAttackAsync(game, request.AttackerCountryCode!, request.DefenderCountryCode!, cancellationToken), cancellationToken);

    [HttpPost("roll")]
    public Task<IActionResult> Roll(string gameId, CancellationToken cancellationToken) =>
        PerformAsync(gameId, game => gameApplicationService.RollAttackAsync(game, cancellationToken), cancellationToken);

    [HttpPost("stop")]
    public Task<IActionResult> Stop(string gameId, CancellationToken cancellationToken) =>
        PerformAsync(gameId, game => gameApplicationService.StopAttackAsync(game, cancellationToken), cancellationToken);

    [HttpPost("end-turn")]
    public Task<IActionResult> EndTurn(string gameId, CancellationToken cancellationToken) =>
        PerformAsync(gameId, game => gameApplicationService.EndTurnAsync(game, cancellationToken), cancellationToken);

    [HttpPost("skip")]
    public Task<IActionResult> Skip(string gameId, CancellationToken cancellationToken) =>
        PerformAsync(gameId, game => gameApplicationService.SkipAsync(game, cancellationToken), cancellationToken);

    /// <summary>Returns the current state, used by the board to catch up after a conflict. Open to spectators like the page itself.</summary>
    [HttpGet("")]
    public async Task<IActionResult> State(string gameId, CancellationToken cancellationToken)
    {
        var game = await gameQueryService.GetGameAsync(gameId, cancellationToken);
        if (game is null)
        {
            return NotFound(new { error = "That game does not exist." });
        }

        if (game.AwaitsNpcTurn)
        {
            npcTurnQueue.Enqueue(game.Id); // self-heal, as when the board page is opened
        }

        var viewerIsOwner = (await authorizationService.AuthorizeAsync(User, game, GamePolicies.CanPlayGame)).Succeeded;
        return Ok(GameStateViewModel.From(game, viewerIsOwner));
    }

    private async Task<IActionResult> PerformAsync(
        string gameId,
        Func<Game, Task<IReadOnlyList<GameEvent>>> action,
        CancellationToken cancellationToken)
    {
        var game = await gameQueryService.GetGameAsync(gameId, cancellationToken);
        if (game is null)
        {
            return NotFound(new { error = "That game does not exist." });
        }

        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new { error = "Log in to play this game." });
        }

        if (!(await authorizationService.AuthorizeAsync(User, game, GamePolicies.CanPlayGame)).Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Only the owner of this game can play it." });
        }

        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(entry => entry.Errors).First().ErrorMessage;
            return BadRequest(new { error = string.IsNullOrEmpty(message) ? "That request could not be read." : message });
        }

        var events = await action(game);
        return Ok(new GameActionResponse(GameStateViewModel.From(game, viewerIsOwner: true), events.Select(GameEventViewModel.From).ToList()));
    }
}
