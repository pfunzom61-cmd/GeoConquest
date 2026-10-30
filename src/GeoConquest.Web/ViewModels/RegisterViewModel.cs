using System.ComponentModel.DataAnnotations;

namespace GeoConquest.Web.ViewModels;

/// <summary>The registration form. The password rules themselves are enforced by the Identity password policy.</summary>
public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Choose a display name.")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Display name must be 2 to 30 characters.")]
    [Display(Name = "Display name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "That is not a valid email address.")]
    [StringLength(254, ErrorMessage = "That email address is too long.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, ErrorMessage = "Password must be 100 characters or fewer.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the password again.")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
