using System.ComponentModel.DataAnnotations;

namespace GeoConquest.Web.ViewModels;

/// <summary>The sign-in form.</summary>
public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "That is not a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }

    /// <summary>Where to go after signing in. Untrusted input: the controller only follows local addresses.</summary>
    public string? ReturnUrl { get; set; }
}
