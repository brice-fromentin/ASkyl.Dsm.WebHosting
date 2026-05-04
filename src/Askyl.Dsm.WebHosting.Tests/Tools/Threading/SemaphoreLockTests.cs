using Askyl.Dsm.WebHosting.Tools.Threading;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Threading;

public class SemaphoreLockTests
{
    #region AcquireAsync - Basic

    [Fact]
    public async Task AcquireAsync_AcquiresSemaphore()
    {
        // Arrange
        var owner = new TestSemaphoreOwner(1);

        // Act
        using var @lock = await SemaphoreLock.AcquireAsync(owner);

        // Assert
        Assert.Equal(0, owner.Semaphore.CurrentCount);
    }

    [Fact]
    public async Task AcquireAsync_Dispose_ReleasesSemaphore()
    {
        // Arrange
        var owner = new TestSemaphoreOwner(1);

        // Act — acquire, then dispose
        var @lock = await SemaphoreLock.AcquireAsync(owner);
        Assert.Equal(0, owner.Semaphore.CurrentCount);
        @lock.Dispose();

        // Assert
        Assert.Equal(1, owner.Semaphore.CurrentCount);
    }

    [Fact]
    public async Task AcquireAsync_NullOwner_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = async () => await SemaphoreLock.AcquireAsync(null!);

        // Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(act);
        Assert.Equal("owner", ex.ParamName);
    }

    #endregion

    #region AcquireAsync - onAcquired Callback

    [Fact]
    public async Task AcquireAsync_OnAcquired_ExecutesCallback()
    {
        // Arrange
        var owner = new TestSemaphoreOwner(1);
        var called = false;

        // Act
        using var @lock = await SemaphoreLock.AcquireAsync(
            owner,
            onAcquired: async () =>
            {
                called = true;
                await Task.Yield();
            });

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task AcquireAsync_OnAcquiredThrow_DisposesLockAndRethrows()
    {
        // Arrange
        var owner = new TestSemaphoreOwner(1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await SemaphoreLock.AcquireAsync(
                owner,
                onAcquired: async () =>
                {
                    await Task.Yield();
                    throw new InvalidOperationException("Callback failed");
                }));

        Assert.Equal("Callback failed", ex.Message);

        // Verify semaphore was released despite the exception
        Assert.Equal(1, owner.Semaphore.CurrentCount);
    }

    #endregion

    #region Dispose - Idempotent

    [Fact]
    public async Task Dispose_CalledTwice_ReleasesOnlyOnce()
    {
        // Arrange
        var owner = new TestSemaphoreOwner(1);
        var @lock = await SemaphoreLock.AcquireAsync(owner);
        Assert.Equal(0, owner.Semaphore.CurrentCount);

        // Act
        @lock.Dispose();
        @lock.Dispose(); // Second call should be no-op

        // Assert
        Assert.Equal(1, owner.Semaphore.CurrentCount);
    }

    #endregion

    #region Concurrency

    [Fact]
    public async Task AcquireAsync_MultipleConcurrent_SerializesExecution()
    {
        // Arrange
        var owner = new TestSemaphoreOwner(1);
        var concurrentCount = 0;
        var maxConcurrent = 0;

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(async _ =>
            {
                using var @lock = await SemaphoreLock.AcquireAsync(owner);
                Interlocked.Increment(ref concurrentCount);
                maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
                await Task.Delay(10);
                Interlocked.Decrement(ref concurrentCount);
            })
            .ToList();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, maxConcurrent);
        Assert.Equal(1, owner.Semaphore.CurrentCount);
    }

    [Fact]
    public async Task Dispose_Concurrent_MultipleThreads_ReleasesOnlyOnce()
    {
        // Arrange
        var owner = new TestSemaphoreOwner(1);
        var @lock = await SemaphoreLock.AcquireAsync(owner);

        // Act — multiple threads call Dispose simultaneously
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => Task.Run(() => @lock.Dispose()))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, owner.Semaphore.CurrentCount);
    }

    #endregion

    #region CancellationToken

    [Fact]
    public async Task AcquireAsync_Cancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var owner = new TestSemaphoreOwner(1);
        using var first = await SemaphoreLock.AcquireAsync(owner);

        var cts = new CancellationTokenSource(100);

        // Act & Assert — second acquire should be cancelled since semaphore is held
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await SemaphoreLock.AcquireAsync(owner, cancellationToken: cts.Token));

        first.Dispose();
    }

    #endregion

    #region Test Fixture

    private class TestSemaphoreOwner(int initialCount) : ISemaphoreOwner
    {
        public SemaphoreSlim Semaphore { get; } = new(initialCount, initialCount);
    }

    #endregion
}
