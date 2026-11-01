using System.Diagnostics;
using GeoConquest.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace GeoConquest.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("how-to-play")]
    public IActionResult HowToPlay() => View();

    /// <summary>Where an unhandled exception ends up (outside development). It keeps the 500 status the exception handler set.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View("StatusPage", StatusPageViewModel.For(StatusCodes.Status500InternalServerError, Activity.Current?.Id ?? HttpContext.TraceIdentifier));

    /// <summary>The friendly page for an empty 403, 404 or 429 response. The original status code is kept.</summary>
    [Route("status/{code:int}")]
    [IgnoreAntiforgeryToken] // the page is re-run with the failed request's own method, often a POST
    public IActionResult StatusPage(int code) => View(StatusPageViewModel.For(code));
}
