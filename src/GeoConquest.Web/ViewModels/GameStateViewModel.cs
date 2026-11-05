using GeoConquest.Core.Constants;
using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;

namespace GeoConquest.Web.ViewModels;

/// <summary>
/// Everything the board needs to draw a game, safe to show to anyone with the link: it carries the owner's display
/// name only, never their user id or email. Also the JSON returned after every action.
/// </summary>
public sealed record GameStateViewModel(
    string GameId,
    string OwnerDisplayName,
    string Status,
    int Round,
    string Phase,
    string CurrentPlayerId,
    string CurrentPlayerName,
    int PendingReinforcements,
    string? WinnerName,
    string NextStep,
    int Version,
    int LastEventSequence,
    bool IsFastForward,
    bool ViewerIsOwner,
    bool CanAct,
    IReadOnlyList<string> AvailableActions,
    IReadOnlyList<PlayerViewModel> Players,
    IReadOnlyList<TerritoryViewModel> Territories,
    BattleViewModel? Battle,
    IReadOnlyList<AttackOddsViewModel> AttackOdds)
{
    /// <param name="game">The game to describe.</param>
    /// <param name="viewerIsOwner">Whether the viewer may play it; spectators get no available actions.</param>
    /// <param name="oddsEstimator">When given, the owner also gets the estimated win chance of each attack they could make.</param>
    public static GameStateViewModel From(Game game, bool viewerIsOwner, IAttackOddsEstimator? oddsEstimator = null)
    {
        var playerNames = game.Players.ToDictionary(player => player.Id, player => player.DisplayName);
        var canAct = viewerIsOwner && game.Status == GameStatus.InProgress && game.CurrentPlayer.IsHuman;

        var battle = game.ActiveBattle is { } active
            ? new BattleViewModel(
                active.AttackerCountryCode,
                game.GetTerritory(active.AttackerCountryCode).CountryName,
                active.DefenderCountryCode,
                game.GetTerritory(active.DefenderCountryCode).CountryName,
                active.Rounds.Count)
            : null;

        return new GameStateViewModel(
            game.Id,
            game.OwnerDisplayName,
            game.Status.ToString(),
            game.RoundNumber,
            game.Phase.ToString(),
            game.CurrentPlayer.Id,
            game.CurrentPlayer.DisplayName,
            game.PendingReinforcements,
            game.WinnerPlayerId is null ? null : playerNames[game.WinnerPlayerId],
            DescribeNextStep(game),
            game.Version,
            game.LastEventSequence,
            game.IsFastForward,
            viewerIsOwner,
            canAct,
            canAct ? ActionsFor(game.Phase) : [],
            game.Players
                .Select(player => new PlayerViewModel(
                    player.Id, player.DisplayName, player.IsHuman, player.IsEliminated, player.ColorKey, player.HomeCountryCode,
                    game.TerritoriesOwnedBy(player.Id).Count))
                .ToList(),
            game.Territories
                .Select(territory => new TerritoryViewModel(
                    territory.CountryCode, territory.CountryName, territory.OwnerPlayerId,
                    playerNames[territory.OwnerPlayerId], territory.Soldiers, territory.SoldierValue))
                .ToList(),
            battle,
            canAct && oddsEstimator is not null ? EstimateOdds(game, oddsEstimator) : []);
    }

    /// <summary>
    /// While choosing an attack: every attack the human could start. While one is under way: that attack at the current
    /// stacks. At any other time there is nothing to estimate.
    /// </summary>
    private static IReadOnlyList<AttackOddsViewModel> EstimateOdds(Game game, IAttackOddsEstimator oddsEstimator)
    {
        if (game.ActiveBattle is { } battle)
        {
            var (attacker, defender) = (game.GetTerritory(battle.AttackerCountryCode), game.GetTerritory(battle.DefenderCountryCode));
            return [new AttackOddsViewModel(attacker.CountryCode, defender.CountryCode, oddsEstimator.Estimate(attacker.Soldiers, defender.Soldiers))];
        }

        if (game.Phase != TurnPhase.Action)
        {
            return [];
        }

        var humanId = game.CurrentPlayer.Id;
        var attackers = game.TerritoriesOwnedBy(humanId).Where(territory => territory.Soldiers >= GameRules.MinimumAttackingSoldiers);
        var targets = game.Territories.Where(territory => territory.OwnerPlayerId != humanId).ToList();

        return attackers
            .SelectMany(attacker => targets.Select(target =>
                new AttackOddsViewModel(attacker.CountryCode, target.CountryCode, oddsEstimator.Estimate(attacker.Soldiers, target.Soldiers))))
            .ToList();
    }

    private static IReadOnlyList<string> ActionsFor(TurnPhase phase) => phase switch
    {
        TurnPhase.Reinforce => ["allocate"],
        TurnPhase.Action => ["transfer", "attack", "skip"],
        TurnPhase.Attacking => ["roll", "stop"],
        TurnPhase.PostAttack => ["transfer", "endTurn"],
        _ => [],
    };

    /// <summary>The hint shown above the action panel; it is also the reason given when an action is unavailable.</summary>
    private static string DescribeNextStep(Game game)
    {
        if (game.Status == GameStatus.Won)
        {
            return $"Victory! {game.OwnerDisplayName} has conquered all ten countries.";
        }

        if (game.Status == GameStatus.Lost)
        {
            return game.WinnerPlayerId is null
                ? $"Defeat. {game.OwnerDisplayName} has been eliminated."
                : $"Defeat. {game.GetPlayer(game.WinnerPlayerId).DisplayName} has conquered the world.";
        }

        if (!game.CurrentPlayer.IsHuman)
        {
            return $"Waiting for {game.CurrentPlayer.DisplayName} to play.";
        }

        return game.Phase switch
        {
            TurnPhase.Reinforce => $"Place {Wording.Count(game.PendingReinforcements, "reinforcement")} before doing anything else.",
            TurnPhase.Action => "Attack a country, move soldiers between your countries, or skip your turn.",
            TurnPhase.Attacking => "Roll the dice, or stop the attack (stopping ends your turn).",
            _ => "The attack is over. Move soldiers if you like, then end your turn.",
        };
    }
}
