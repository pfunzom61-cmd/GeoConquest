using GeoConquest.Infrastructure.Identity;
using GeoConquest.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GeoConquest.Web.Controllers;

/// <summary>Registration, sign-in and sign-out. Identity does the work; this controller only translates results for the views.</summary>
public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    // The same message for an unknown email and a wrong password, so the form cannot be used to discover accounts.
    private const string InvalidSignInMessage = "That email and password do not match.";
    private const string LockedOutMessage = "Too many failed attempts. This account is locked for 15 minutes.";

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, DisplayName = model.DisplayName };
        var result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            // The email is also the username, so a taken email would otherwise report the same problem twice.
            var errors = result.Errors.Where(error => error.Code != nameof(IdentityErrorDescriber.DuplicateUserName));
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Only local addresses: a crafted link must not be able to send a signed-in player to another site.
            return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl) : RedirectToAction(nameof(Profile));
        }

        ModelState.AddModelError(string.Empty, result.IsLockedOut ? LockedOutMessage : InvalidSignInMessage);
        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(new ProfileViewModel(user.DisplayName, user.Email ?? string.Empty));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
