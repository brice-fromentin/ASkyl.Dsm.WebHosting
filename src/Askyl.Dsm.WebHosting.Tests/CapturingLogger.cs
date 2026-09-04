using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tests;

/// <summary>
/// Records the messages a service logs, so a test can assert on what a log reader would see.
/// </summary>
/// <remarks>
/// A real implementation rather than a mock on purpose. Verifying a Moq expectation means naming
/// <c>ILogger.Log</c> in an expression, which ADWH03001 rejects — correctly, since it cannot tell an
/// assertion from a call. Implementing the interface sidesteps that without suppressing the rule, and
/// <c>IsEnabled</c> returning true matters: a mock answers false by default, and the source-generated
/// methods then log nothing at all.
/// </remarks>
public sealed class CapturingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Messages.Add(formatter(state, exception));
}
