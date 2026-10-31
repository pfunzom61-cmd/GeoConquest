using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Infrastructure.AI;
using GeoConquest.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GeoConquest.Infrastructure.Tests.AI;

public sealed class GroqCommentaryServiceTests
{
    private const string ApiKey = "test-key-value-that-must-stay-secret";

    private static readonly BattleContext Round = new(
        "Brazil", "Chile", 8, 7, 3, 2, [6, 2], [5, 3], 1, 1, 3, 2, 2, 4, BattleOutcome.Continuing);

    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(5);

    private static GroqCommentaryService CreateService(FakeGroqHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://groq.test/openai/v1/") },
        Options.Create(new GroqOptions { ApiKey = ApiKey }),
        NullLogger<GroqCommentaryService>.Instance);

    private static HttpResponseMessage Answer(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task DescribeRoundAsync_SendsTheBattleFactsToChatCompletionsAndReturnsTheAnswer()
    {
        var handler = new FakeGroqHandler(() => Answer("""{"choices":[{"message":{"role":"assistant","content":"Brazil surges!"},"finish_reason":"stop"}]}"""));

        var commentary = await CreateService(handler).DescribeRoundAsync(Round, Budget, CancellationToken.None);

        Assert.Equal(new GeneratedCommentary("Brazil surges!", CommentarySource.Ai), commentary);
        Assert.Equal("https://groq.test/openai/v1/chat/completions", handler.LastUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", ApiKey), handler.LastAuthorization);

        var body = JsonDocument.Parse(handler.LastBody!).RootElement;
        Assert.Equal("openai/gpt-oss-20b", body.GetProperty("model").GetString());
        Assert.Equal(400, body.GetProperty("max_completion_tokens").GetInt32());
        Assert.Equal("low", body.GetProperty("reasoning_effort").GetString());
        Assert.False(body.GetProperty("include_reasoning").GetBoolean());
        Assert.False(body.TryGetProperty("max_tokens", out _));
        Assert.Contains("Brazil is attacking Chile.", body.GetProperty("messages")[0].GetProperty("content").GetString());
    }

    public static TheoryData<HttpResponseMessage, TimeSpan?> FailedAnswers()
    {
        var rateLimited = Answer("{}", HttpStatusCode.TooManyRequests);
        rateLimited.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(17));

        return new TheoryData<HttpResponseMessage, TimeSpan?>
        {
            { rateLimited, TimeSpan.FromSeconds(17) },
            { Answer("{}", HttpStatusCode.TooManyRequests), TimeSpan.FromMinutes(1) },
            { Answer("{}", HttpStatusCode.Unauthorized), TimeSpan.FromMinutes(5) },
            { Answer("{}", HttpStatusCode.ServiceUnavailable), null },
            { Answer("""{"choices":[{"message":{"content":""},"finish_reason":"length"}]}"""), null },
        };
    }

    [Theory]
    [MemberData(nameof(FailedAnswers))]
    public async Task DescribeRoundAsync_NoUsableAnswer_ReportsHowLongToWaitBeforeAskingAgain(HttpResponseMessage response, TimeSpan? expectedPause)
    {
        var service = CreateService(new FakeGroqHandler(() => response));

        var failure = await Assert.ThrowsAsync<CommentaryProviderException>(
            () => service.DescribeRoundAsync(Round, Budget, CancellationToken.None));

        Assert.Equal(expectedPause, failure.RetryAfter);
    }

    [Fact]
    public async Task Commentary_RequestRefusedWithEverythingLogged_NeverLogsTheApiKeyOrHeaders()
    {
        var handler = new FakeGroqHandler(() => Answer("{}", HttpStatusCode.Unauthorized));
        var logs = new ConcurrentQueue<string>();
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace).AddProvider(new RecordingLoggerProvider(logs)));
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MongoDb:ConnectionString"] = "mongodb://unused.invalid:27017",
            ["MongoDb:DatabaseName"] = "unused",
            ["Groq:ApiKey"] = ApiKey,
        }).Build());
        services.AddHttpClient<GroqCommentaryService>().ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();

        var commentary = await provider.GetRequiredService<ICommentaryService>().DescribeRoundAsync(Round, Budget, CancellationToken.None);

        Assert.Equal(CommentarySource.Template, commentary.Source);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", ApiKey), handler.LastAuthorization);
        var logged = string.Join('\n', logs);
        Assert.Contains("401", logged);
        Assert.DoesNotContain(ApiKey, logged);
        Assert.DoesNotContain("Authorization", logged);
        Assert.DoesNotContain("Bearer", logged);
    }

    private sealed class FakeGroqHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        public string? LastUri { get; private set; }

        public AuthenticationHeaderValue? LastAuthorization { get; private set; }

        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri!.ToString();
            LastAuthorization = request.Headers.Authorization;
            LastBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return respond();
        }
    }

    private sealed class RecordingLoggerProvider(ConcurrentQueue<string> logs) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new RecordingLogger(logs);

        public void Dispose()
        {
        }
    }

    private sealed class RecordingLogger(ConcurrentQueue<string> logs) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            logs.Enqueue($"{formatter(state, exception)} {exception}");
    }
}
