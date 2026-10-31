using GeoConquest.Web.BackgroundServices;

namespace GeoConquest.Web.Tests;

public sealed class NpcTurnQueueTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Enqueue_SameGameTwiceBeforeItIsPlayed_QueuesItOnce()
    {
        var queue = new NpcTurnQueue();
        using var timeout = new CancellationTokenSource(Patience);
        await using var games = queue.DequeueAllAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);

        queue.Enqueue("game-a");
        queue.Enqueue("game-a");
        queue.Enqueue("game-b");

        Assert.True(await games.MoveNextAsync());
        Assert.Equal("game-a", games.Current);
        queue.Finished("game-a");
        Assert.True(await games.MoveNextAsync());
        Assert.Equal("game-b", games.Current);
    }

    [Fact]
    public async Task Enqueue_WhileTheGameIsBeingPlayed_PlaysItAgainOnceFinished()
    {
        var queue = new NpcTurnQueue();
        using var timeout = new CancellationTokenSource(Patience);
        await using var games = queue.DequeueAllAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);
        queue.Enqueue("game-a");
        Assert.True(await games.MoveNextAsync());

        queue.Enqueue("game-a");
        queue.Finished("game-a");

        Assert.True(await games.MoveNextAsync());
        Assert.Equal("game-a", games.Current);
    }
}
