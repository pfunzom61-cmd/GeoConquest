using System.ComponentModel.DataAnnotations;

namespace GeoConquest.Web.ViewModels;

/// <summary>Soldiers to place, keyed by country code. The server checks the total and ownership.</summary>
public sealed class AllocateReinforcementsRequest
{
    [Required(ErrorMessage = "Say where to place your reinforcements.")]
    public Dictionary<string, int>? Allocations { get; set; }
}
