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
    /// Small on purpose: 25 requests a minute of about 170 prompt tokens plus this many stays within the 8,000 tokens
    /// a minute Groq publishes for this model. Groq's docs do not say whether a reasoning model's thinking counts
    /// towards it, so raise this if the log warns about empty answers.
    /// </summary>
    public int MaxCompletionTokens { get; set; } = 150;

    public double Temperature { get; set; } = 0.7;

    /// <summary>Only GPT-OSS models accept this. Leave it empty for another model, and neither reasoning setting is sent.</summary>
    public string ReasoningEffort { get; set; } = "low";

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
}
