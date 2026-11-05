using System.ComponentModel.DataAnnotations;

namespace GeoConquest.Web.ViewModels;

/// <summary>The New Campaign form. Only the chosen country is posted; the list is rebuilt on every display.</summary>
public sealed class NewCampaignViewModel
{
    [Required(ErrorMessage = "Choose a country to command.")]
    public string? SelectedCountryCode { get; set; }

    public IReadOnlyList<CountryOptionViewModel> Countries { get; set; } = [];
}
