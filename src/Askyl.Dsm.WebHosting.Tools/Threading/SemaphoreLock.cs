namespace Askyl.Dsm.WebHosting.Tools.Threading;

public sealed class SemaphoreLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;


    public SemaphoreLock(SemaphoreSlim semaphore)
    {
        ArgumentNullException.ThrowIfNull(semaphore);
        _semaphore = semaphore;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _semaphore.Release();
        _disposed = true;
    }

    public static SemaphoreLock Acquire(SemaphoreSlim semaphore)
    {
        ArgumentNullException.ThrowIfNull(semaphore);
        semaphore.Wait();
        return new SemaphoreLock(semaphore);
    }

    public static async Task<SemaphoreLock> AcquireAsync(SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(semaphore);
        await semaphore.WaitAsync(cancellationToken);
        return new SemaphoreLock(semaphore);
    }


    public static async Task<SemaphoreLock> AcquireAsync(SemaphoreSlim semaphore, Func<Task>? onAcquired = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(semaphore);


        await semaphore.WaitAsync(cancellationToken);
        var lockInstance = new SemaphoreLock(semaphore);

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
            lockInstance.Dispose();
            throw;
        }
    }
}
