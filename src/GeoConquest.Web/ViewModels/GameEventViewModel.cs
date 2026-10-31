using GeoConquest.Core.Models;

namespace GeoConquest.Web.ViewModels;

/// <summary>One line of the event log. The message already spells out dice, losses and outcomes.</summary>
public sealed record GameEventViewModel(int Sequence, string Type, string Message)
{
    public static GameEventViewModel From(GameEvent gameEvent) =>
        new(gameEvent.Sequence, gameEvent.Type.ToString(), gameEvent.Message);
}
