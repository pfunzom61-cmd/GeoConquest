using System.ComponentModel.DataAnnotations;

namespace GeoConquest.Web.ViewModels;

public sealed class TransferRequest
{
    [Required(ErrorMessage = "Choose the country to move soldiers from.")]
    public string? FromCountryCode { get; set; }

    [Required(ErrorMessage = "Choose the country to move soldiers to.")]
    public string? ToCountryCode { get; set; }

    [Range(1, 1000, ErrorMessage = "Move at least one soldier.")]
    public int Soldiers { get; set; }
}
