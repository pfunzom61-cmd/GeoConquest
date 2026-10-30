using GeoConquest.Core.Exceptions;

namespace GeoConquest.Core.Models;

/// <summary>
/// A country on the board during a game: who owns it and how many soldiers it holds.
/// The soldier count can only change through guarded methods so it never goes negative.
/// </summary>
public sealed class Territory
{
    public Territory(string countryCode, string countryName, int soldierValue, string ownerPlayerId, int soldiers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPlayerId);
        ArgumentOutOfRangeException.ThrowIfLessThan(soldierValue, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(soldiers, 1);

        CountryCode = countryCode;
        CountryName = countryName;
        SoldierValue = soldierValue;
        OwnerPlayerId = ownerPlayerId;
        Soldiers = soldiers;
    }

    public string CountryCode { get; }

    public string CountryName { get; }

    /// <summary>Fixed tier value from the country's population. Never changes; used for the conquest reward.</summary>
    public int SoldierValue { get; }

    public string OwnerPlayerId { get; private set; }

    public int Soldiers { get; private set; }

    public void AddSoldiers(int count)
    {
        if (count < 1)
        {
            throw new GameRuleException("You must add at least one soldier.");
        }

        Soldiers += count;
    }

    public void RemoveSoldiers(int count)
    {
        if (count < 1)
        {
            throw new GameRuleException("You must remove at least one soldier.");
        }

        if (count > Soldiers)
        {
            throw new GameRuleException($"{CountryName} only has {Soldiers} soldiers; cannot remove {count}.");
        }

        Soldiers -= count;
    }

    /// <summary>Hands the country to a new owner, replacing its garrison (used when it is conquered).</summary>
    public void ChangeOwner(string newOwnerPlayerId, int soldiers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newOwnerPlayerId);

        if (soldiers < 1)
        {
            throw new GameRuleException("A conquered country must hold at least one soldier.");
        }

        OwnerPlayerId = newOwnerPlayerId;
        Soldiers = soldiers;
    }
}
