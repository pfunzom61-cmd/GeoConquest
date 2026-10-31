using System.Net;
using System.Text.RegularExpressions;
using GeoConquest.Web.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GeoConquest.Web.Tests;

public sealed partial class AccountTests : IClassFixture<GeoConquestWebFactory>
{
    private const string ValidPassword = "Conquest1";

    private readonly GeoConquestWebFactory _factory;

    public AccountTests(GeoConquestWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ValidDetails_CreatesAccountAndSignsIn()
    {
        var client = NewClient();

        var response = await RegisterAsync(client, UniqueEmail(), displayName: "General Bob");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var profile = await client.GetAsync("/Account/Profile");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        Assert.Contains("General Bob", await profile.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("Short1A", "at least 8 characters")]
    [InlineData("nouppercase1", "uppercase")]
    [InlineData("NOLOWERCASE1", "lowercase")]
    [InlineData("NoDigitsHere", "digit")]
    public async Task Register_PasswordBreaksPolicy_IsRejectedWithReason(string password, string expectedReason)
    {
        var client = NewClient();

        var response = await RegisterAsync(client, UniqueEmail(), password: password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedReason, await ValidationSummaryAsync(response), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_EmailAlreadyUsed_IsRejected()
    {
        var email = UniqueEmail();
        await RegisterAsync(NewClient(), email);

        var response = await RegisterAsync(NewClient(), email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("is already taken", await ValidationSummaryAsync(response));
    }

    [Theory]
    [InlineData("/Home/Index", "/Home/Index")]
    [InlineData("https://evil.example/phish", "/Account/Profile")]
    public async Task Login_ValidCredentials_SignsInAndFollowsOnlyLocalReturnUrls(string returnUrl, string expectedLocation)
    {
        var email = UniqueEmail();
        await RegisterAsync(NewClient(), email);

        var response = await LoginAsync(NewClient(), email, ValidPassword, returnUrl);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedLocation, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Login_WrongPasswordOrUnknownEmail_GiveTheSameMessage()
    {
        var email = UniqueEmail();
        await RegisterAsync(NewClient(), email);

        var wrongPassword = await ValidationSummaryAsync(await LoginAsync(NewClient(), email, "WrongPass1"));
        var unknownEmail = await ValidationSummaryAsync(await LoginAsync(NewClient(), UniqueEmail(), "WrongPass1"));

        Assert.Contains("do not match", wrongPassword);
        Assert.Contains("do not match", unknownEmail);
    }

    [Fact]
    public async Task Login_FiveFailedAttempts_LocksAccountEvenForTheCorrectPassword()
    {
        var email = UniqueEmail();
        await RegisterAsync(NewClient(), email);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await LoginAsync(NewClient(), email, "WrongPass1");
        }

        var response = await LoginAsync(NewClient(), email, ValidPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("locked", await ValidationSummaryAsync(response));
    }

    [Fact]
    public async Task Logout_SignedIn_EndsTheSession()
    {
        var client = NewClient();
        await RegisterAsync(client, UniqueEmail());

        await client.SubmitFormAsync("/Account/Profile", "/Account/Logout", new Dictionary<string, string>());

        var profile = await client.GetAsync("/Account/Profile");
        Assert.Equal(HttpStatusCode.Redirect, profile.StatusCode);
    }

    [Fact]
    public async Task Profile_Anonymous_RedirectsToLogin()
    {
        var response = await NewClient().GetAsync("/Account/Profile");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", response.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task Post_WithoutAntiforgeryToken_IsRejected()
    {
        var fields = new Dictionary<string, string> { ["Email"] = UniqueEmail(), ["Password"] = ValidPassword };

        var response = await NewClient().PostWithoutTokenAsync("/Account/Login", fields);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_TooManySubmissionsFromOneAddress_AreThrottled()
    {
        const int permitLimit = 2;
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:Auth:PermitLimit", permitLimit.ToString()));
        var client = new FormPostingClient(factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }));

        for (var attempt = 0; attempt < permitLimit; attempt++)
        {
            await LoginAsync(client, UniqueEmail(), "WrongPass1");
        }

        var response = await LoginAsync(client, UniqueEmail(), "WrongPass1");

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Login_ThrottleIsPerForwardedClientAddress_NotPerProxy()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "1"));
        var firstVisitor = NewClientBehindProxy(factory, "203.0.113.10");
        var secondVisitor = NewClientBehindProxy(factory, "203.0.113.20");

        await LoginAsync(firstVisitor, UniqueEmail(), "WrongPass1");
        var firstVisitorAgain = await LoginAsync(firstVisitor, UniqueEmail(), "WrongPass1");
        var secondVisitorFirstTry = await LoginAsync(secondVisitor, UniqueEmail(), "WrongPass1");

        Assert.Equal(HttpStatusCode.TooManyRequests, firstVisitorAgain.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, secondVisitorFirstTry.StatusCode);
    }

    private static FormPostingClient NewClientBehindProxy(WebApplicationFactory<Program> factory, string clientAddress)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", clientAddress);
        return new FormPostingClient(client);
    }

    private FormPostingClient NewClient() =>
        new(_factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }));

    private static string UniqueEmail() => $"{Guid.NewGuid():N}@example.org";

    private static Task<HttpResponseMessage> RegisterAsync(
        FormPostingClient client, string email, string displayName = "Bob", string password = ValidPassword) =>
        client.SubmitFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["DisplayName"] = displayName,
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
        });

    private static Task<HttpResponseMessage> LoginAsync(FormPostingClient client, string email, string password, string? returnUrl = null) =>
        client.SubmitFormAsync("/Account/Login", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ReturnUrl"] = returnUrl ?? string.Empty,
        });

    // Only the rendered errors: the register page always shows the password rules, which would match anything.
    private static async Task<string> ValidationSummaryAsync(HttpResponseMessage response)
    {
        var summary = ValidationErrors().Match(await response.Content.ReadAsStringAsync());
        return WebUtility.HtmlDecode(summary.Groups[1].Value);
    }

    [GeneratedRegex("""class="validation-summary-errors"[^>]*>(.*?)</div>""", RegexOptions.Singleline)]
    private static partial Regex ValidationErrors();
}
