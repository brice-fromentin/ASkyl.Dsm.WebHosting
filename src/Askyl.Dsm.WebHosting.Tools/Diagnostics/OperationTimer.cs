using System.Diagnostics;

namespace Askyl.Dsm.WebHosting.Tools.Diagnostics;

/// <summary>
/// A value-type timer that starts on construction and invokes a callback with elapsed milliseconds on disposal.
/// Use with <c>using var</c> to guarantee duration capture on all exit paths (success, exception, early return).
/// </summary>
/// <remarks>
/// Creates a new timer and immediately starts it.
/// </remarks>
/// <param name="onElapsed">Callback invoked on <see cref="Stop"/> with elapsed milliseconds.</param>
public struct OperationTimer(Action<long> onElapsed) : IDisposable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly Action<long> _onElapsed = onElapsed ?? (_ => { });
    private bool _disposed = false;

    /// <summary>
    /// The elapsed time in milliseconds (readable at any point during the operation).
    /// </summary>
    public readonly long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    /// <summary>
    /// Stops the timer and invokes the elapsed callback exactly once.
    /// Idempotent — safe to call multiple times or from <see cref="IDisposable.Dispose"/>.
    /// </summary>
    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        var elapsed = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Stop();
        _disposed = true;

        _onElapsed(elapsed);
    }

    void IDisposable.Dispose() => Stop();

    /// <summary>
    /// Stops the timer and invokes the elapsed callback exactly once.
    /// Equivalent to <see cref="Stop"/> — provided for <see cref="IDisposable"/> compatibility.
    /// </summary>
    public void Dispose() => Stop();
}
