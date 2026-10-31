using System.ComponentModel.DataAnnotations;

namespace GeoConquest.Web.ViewModels;

public sealed class AttackRequest
{
    [Required(ErrorMessage = "Choose the country to attack from.")]
    public string? AttackerCountryCode { get; set; }

    [Required(ErrorMessage = "Choose the country to attack.")]
    public string? DefenderCountryCode { get; set; }
}
