namespace Askyl.Dsm.WebHosting.Tools.Threading;

/// <summary>
/// Interface for components that own a semaphore for thread-safe operations.
/// Similar to IWorkingState pattern - forces explicit ownership declaration.
/// </summary>
public interface ISemaphoreOwner
{
    /// <summary>
    /// Gets the semaphore used for synchronization.
    /// </summary>
    SemaphoreSlim Semaphore { get; }
}

/// <summary>
/// Disposable semaphore lock wrapper that ensures automatic release via using statement.
/// Follows the same pattern as WorkingState for consistent API design.
/// </summary>
public sealed class SemaphoreLock : IDisposable
{
    private readonly ISemaphoreOwner _owner;
    private int _disposed = 0;

    /// <summary>
    /// Private constructor forces usage through static factory methods only.
    /// </summary>
    private SemaphoreLock(ISemaphoreOwner owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
    }

    /// <summary>
    /// Releases the underlying semaphore. Thread-safe and safe to call multiple times.
    /// Uses Interlocked.CompareExchange for thread safety.
    /// </summary>
    public void Dispose()
    {
        // Use Interlocked.CompareExchange to ensure only one thread releases the semaphore
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            _owner.Semaphore.Release();
        }
    }

    /// <summary>
    /// Asynchronously acquires the semaphore and returns a disposable lock wrapper.
    /// Optionally executes initialization callback after acquiring lock.
    /// Use with 'using' statement to ensure automatic release.
    /// </summary>
    /// <remarks>
    /// If the onAcquired callback throws an exception, the lock will be disposed before rethrowing.
    /// The returned instance should not be used in this case as it has been disposed.
    /// </remarks>
    public static async Task<SemaphoreLock> AcquireAsync(
        ISemaphoreOwner owner,
        Func<Task>? onAcquired = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(owner);

        await owner.Semaphore.WaitAsync(cancellationToken);
        var lockInstance = new SemaphoreLock(owner);

        try
        {
            if (onAcquired != null)
            {
                await onAcquired();
            }

            return lockInstance;
        }
        catch
        {
            // Dispose on any exception to prevent semaphore leak during initialization
            lockInstance.Dispose();
            throw;
        }
    }
}
