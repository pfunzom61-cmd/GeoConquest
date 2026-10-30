namespace GeoConquest.Core.Interfaces;

/// <summary>Source of dice rolls. Abstracted so tests can supply fixed values.</summary>
public interface IDiceRoller
{
    /// <summary>Rolls <paramref name="count"/> six-sided dice and returns them in roll order.</summary>
    IReadOnlyList<int> Roll(int count);
}
