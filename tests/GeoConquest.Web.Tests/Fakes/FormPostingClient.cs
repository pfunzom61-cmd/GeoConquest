using System.Net;
using System.Text.RegularExpressions;

namespace GeoConquest.Web.Tests.Fakes;

/// <summary>Submits forms the way a browser does: loads a page first and sends back its anti-forgery token.</summary>
public sealed partial class FormPostingClient(HttpClient client)
{
    public Task<HttpResponseMessage> GetAsync(string url) => client.GetAsync(url);

    /// <summary>Loads the form at <paramref name="url"/> and posts the fields back to the same address.</summary>
    public Task<HttpResponseMessage> SubmitFormAsync(string url, IDictionary<string, string> fields) =>
        SubmitFormAsync(url, url, fields);

    /// <summary>Takes the token from <paramref name="pageUrl"/> and posts to <paramref name="actionUrl"/>, as the logout button does.</summary>
    public async Task<HttpResponseMessage> SubmitFormAsync(string pageUrl, string actionUrl, IDictionary<string, string> fields)
    {
        var page = await client.GetStringAsync(pageUrl);
        var token = WebUtility.HtmlDecode(AntiforgeryTokenField().Match(page).Groups[1].Value);

        var body = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = token };
        return await client.PostAsync(actionUrl, new FormUrlEncodedContent(body));
    }

    /// <summary>Posts without a token, which is what a forged cross-site request looks like.</summary>
    public Task<HttpResponseMessage> PostWithoutTokenAsync(string url, IDictionary<string, string> fields) =>
        client.PostAsync(url, new FormUrlEncodedContent(fields));

    [GeneratedRegex("""name="__RequestVerificationToken" type="hidden" value="([^"]+)" """)]
    private static partial Regex AntiforgeryTokenField();
}
