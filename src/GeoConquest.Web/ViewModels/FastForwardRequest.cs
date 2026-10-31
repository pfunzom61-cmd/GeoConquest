using System.ComponentModel.DataAnnotations;

namespace GeoConquest.Web.ViewModels;

public sealed class FastForwardRequest
{
    // Nullable so that a missing value is rejected rather than silently read as "off".
    [Required(ErrorMessage = "Say whether fast-forward should be on or off.")]
    public bool? Enabled { get; set; }
}
