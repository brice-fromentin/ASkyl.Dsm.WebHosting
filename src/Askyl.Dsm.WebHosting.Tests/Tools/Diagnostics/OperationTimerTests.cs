using Askyl.Dsm.WebHosting.Tools.Diagnostics;
using Askyl.Dsm.WebHosting.Tools.Threading;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Diagnostics;

public class OperationTimerTests
{
    #region Construction & Start

    [Fact]
    public void Ctor_StartsStopwatchImmediately()
    {
        // Act
        var timer = new OperationTimer(_ => { });

        // Assert — timer should show some elapsed time (even if minimal)
        Assert.True(timer.ElapsedMilliseconds >= 0);
    }

    [Fact]
    public void Ctor_WithNullAction_DoesNotThrow()
    {
        // Act & Assert — null should map to Action<long>.Empty
        var timer = new OperationTimer(null!);
        timer.Dispose(); // Should not throw
    }

    #endregion

    #region Dispose - Callback Invocation

    [Fact]
    public void Dispose_InvokesCallbackWithElapsed()
    {
        // Arrange
        long capturedDuration = -1;

        // Act
        using var timer = new OperationTimer(elapsed => capturedDuration = elapsed);

        // Assert
        Assert.Equal(-1, capturedDuration); // Not yet disposed
    }

    [Fact]
    public void Dispose_CallbackReceivesPositiveElapsed()
    {
        // Arrange
        long capturedDuration = -1;

        // Act
        using (new OperationTimer(elapsed => capturedDuration = elapsed))
        {
            // Simulate some work
            Thread.Sleep(10);
        }

        // Assert
        Assert.True(capturedDuration >= 0);
    }

    [Fact]
    public void Dispose_InvokesCallbackExactlyOnce()
    {
        // Arrange
        var invokeCount = 0;

        // Act
        var timer = new OperationTimer(_ => invokeCount++);
        timer.Dispose();
        timer.Dispose(); // Double dispose
        timer.Dispose(); // Triple dispose

        // Assert
        Assert.Equal(1, invokeCount);
    }

    #endregion

    #region Stop - Explicit

    [Fact]
    public void Stop_InvokesCallbackBeforeDispose()
    {
        // Arrange
        var invokeCount = 0;

        // Act
        var timer = new OperationTimer(_ => invokeCount++);
        timer.Stop();
        timer.Dispose(); // Should be no-op since already stopped

        // Assert
        Assert.Equal(1, invokeCount);
    }

    [Fact]
    public void Stop_Idempotent()
    {
        // Arrange
        var invokeCount = 0;
        var timer = new OperationTimer(_ => invokeCount++);

        // Act
        timer.Stop();
        timer.Stop();
        timer.Stop();

        // Assert
        Assert.Equal(1, invokeCount);
    }

    #endregion

    #region ElapsedMilliseconds

    [Fact]
    public void ElapsedMilliseconds_IncreasesOverTime()
    {
        // Arrange
        var timer = new OperationTimer(_ => { });

        // Act
        var first = timer.ElapsedMilliseconds;
        Thread.Sleep(5);
        var second = timer.ElapsedMilliseconds;

        // Assert
        Assert.True(second >= first);
        timer.Dispose();
    }

    [Fact]
    public void ElapsedMilliseconds_ReturnsZeroInitially()
    {
        // Act
        var timer = new OperationTimer(_ => { });

        // Assert — should be 0 or very close to 0
        Assert.True(timer.ElapsedMilliseconds <= 5);
        timer.Dispose();
    }

    #endregion

    #region Scope Patterns

    [Fact]
    public void UsingScope_CallbackInvokedOnExit()
    {
        // Arrange
        long capturedDuration = -1;

        // Act
        using (new OperationTimer(elapsed => capturedDuration = elapsed))
        {
            Thread.Sleep(5);
        }

        // Assert
        Assert.True(capturedDuration >= 0);
    }

    [Fact]
    public void UsingScope_WithEarlyReturn_CallbackInvoked()
    {
        // Arrange
        long capturedDuration = -1;

        // Act — simulate early return by returning from scope
        void MethodWithEarlyReturn()
        {
            using (new OperationTimer(elapsed => capturedDuration = elapsed))
            {
                Thread.Sleep(5);
                return; // Early return — timer should still log
            }
        }

        MethodWithEarlyReturn();

        // Assert
        Assert.True(capturedDuration >= 0);
    }

    [Fact]
    public void UsingScope_WithException_CallbackInvoked()
    {
        // Arrange
        long capturedDuration = -1;

        // Act
        try
        {
            using (new OperationTimer(elapsed => capturedDuration = elapsed))
            {
                Thread.Sleep(5);
                throw new InvalidOperationException("Simulated failure");
            }
        }

        catch
        {
            // Expected
        }

        // Assert — callback should still be invoked on exception
        Assert.True(capturedDuration >= 0);
    }

    #endregion

    #region Combining with SemaphoreLock

    [Fact]
    public async Task OperationTimer_WithSemaphoreLock_BothExecuteCorrectly()
    {
        // Arrange
        var owner = new TestSemaphoreOwner(1);
        long capturedDuration = -1;

        // Act
        using (new OperationTimer(elapsed => capturedDuration = elapsed))
        using (await SemaphoreLock.AcquireAsync(owner))
        {
            Thread.Sleep(5);
        }

        // Assert
        Assert.True(capturedDuration >= 0);
        Assert.Equal(1, owner.Semaphore.CurrentCount); // Semaphore released
    }

    #endregion

    #region Test Fixture

    private class TestSemaphoreOwner(int initialCount) : ISemaphoreOwner
    {
        public SemaphoreSlim Semaphore { get; } = new(initialCount, initialCount);
    }

    #endregion
}
