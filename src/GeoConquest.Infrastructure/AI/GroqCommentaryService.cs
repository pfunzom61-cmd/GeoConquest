using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeoConquest.Infrastructure.AI;

/// <summary>
/// Asks Groq's OpenAI-compatible chat endpoint for commentary and reports every failure as a
/// <see cref="CommentaryProviderException"/>. Timing, rate limiting and checking the answer belong to
/// <see cref="ResilientCommentaryService"/>, which cancels this call when the time budget runs out. Logs here carry
/// status codes and the model name only: never the API key, a header, or the body of a request or response.
/// </summary>
public sealed class GroqCommentaryService(
    HttpClient httpClient,
    IOptions<GroqOptions> options,
    ILogger<GroqCommentaryService> logger) : ICommentaryService
{
    private static readonly TimeSpan DefaultRateLimitPause = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RejectedRequestPause = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<GeneratedCommentary> DescribeRoundAsync(BattleContext context, TimeSpan timeBudget, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(context);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning("Could not reach Groq: {Reason}.", exception.HttpRequestError);
            throw new CommentaryProviderException("Could not reach Groq.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw Failure(response);
            }

            var answer = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions, cancellationToken);
            var choice = answer?.Choices?.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(choice?.Message?.Content))
            {
                logger.LogWarning(
                    "Groq returned an empty answer (finish reason {FinishReason}). If it is 'length', raise Groq:MaxCompletionTokens.",
                    choice?.FinishReason);
                throw new CommentaryProviderException("Groq returned an empty answer.");
            }

            return new GeneratedCommentary(choice.Message.Content, CommentarySource.Ai);
        }
    }

    private HttpRequestMessage BuildRequest(BattleContext context)
    {
        var settings = options.Value;
        var usesReasoningSettings = !string.IsNullOrWhiteSpace(settings.ReasoningEffort);

        // Groq's reasoning guide advises putting every instruction in the user message rather than a system prompt.
        var body = new ChatRequest(
            settings.Model,
            [new ChatMessage("user", CommentaryPromptBuilder.Build(context))],
            settings.Temperature,
            settings.MaxCompletionTokens,
            usesReasoningSettings ? settings.ReasoningEffort : null,
            usesReasoningSettings ? false : null);

        return new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(body, options: JsonOptions),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey) },
        };
    }

    private CommentaryProviderException Failure(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;

        switch (response.StatusCode)
        {
            case HttpStatusCode.TooManyRequests:
                var pause = RetryAfter(response) ?? DefaultRateLimitPause;
                logger.LogWarning("Groq rate limit reached; using templates for {Seconds} seconds.", pause.TotalSeconds);
                return new CommentaryProviderException("Groq rate limit reached.", pause);

            case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound:
                logger.LogError(
                    "Groq refused the request with status {Status}; using templates for a while. Check Groq:ApiKey and Groq:Model (now {Model}).",
                    status, options.Value.Model);
                return new CommentaryProviderException("Groq refused the request.", RejectedRequestPause);

            default:
                logger.LogWarning("Groq answered with status {Status}.", status);
                return new CommentaryProviderException("Groq could not answer.");
        }
    }

    private static TimeSpan? RetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        return retryAfter?.Delta ?? (retryAfter?.Date - DateTimeOffset.UtcNow);
    }

    private sealed record ChatRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        double Temperature,
        int MaxCompletionTokens,
        string? ReasoningEffort,
        bool? IncludeReasoning);

    private sealed record ChatMessage(string Role, string? Content);

    private sealed record ChatResponse(IReadOnlyList<ChatChoice>? Choices);

    private sealed record ChatChoice(ChatMessage? Message, string? FinishReason);
}
