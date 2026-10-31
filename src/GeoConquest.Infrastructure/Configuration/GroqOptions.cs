namespace GeoConquest.Infrastructure.Configuration;

/// <summary>
/// Settings for the Groq language model. The API key is a secret: it comes from user-secrets locally and from the
/// <c>Groq__ApiKey</c> environment variable in production, never from appsettings. It is optional, because without
/// one the game simply uses template commentary.
/// </summary>
public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "openai/gpt-oss-20b";

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";

    /// <summary>
    /// Covers the model's hidden thinking as well as its answer of about 60 tokens: at 150, thinking used up the limit
    /// on 4 of 15 live calls and the answer came back empty. With a prompt of about 350 tokens, a request costs at most
    /// about 750, which is what <c>Commentary:MaxRequestsPerMinute</c> is set from. If this is raised, lower that to
    /// 8,000 / (350 + this).
    /// </summary>
    public int MaxCompletionTokens { get; set; } = 400;

    public double Temperature { get; set; } = 0.7;

    /// <summary>Only GPT-OSS models accept this. Leave it empty for another model, and neither reasoning setting is sent.</summary>
    public string ReasoningEffort { get; set; } = "low";

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
}
