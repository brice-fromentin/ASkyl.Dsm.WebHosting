using System.Diagnostics;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Abstracts process spawning to enable unit testing without real process creation.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Starts a new process with the specified configuration.
    /// </summary>
    /// <param name="startInfo">The start configuration for the process.</param>
    /// <returns>A handle to the running process.</returns>
    IProcessHandle Start(ProcessStartInfo startInfo);
}

/// <summary>
/// Production implementation of <see cref="IProcessRunner"/> that spawns real OS processes.
/// </summary>
/// <param name="logger">Logger instance.</param>
public sealed class SystemProcessRunner(ILogger<ILogSystemProcessRunner> logger) : IProcessRunner
{
    public IProcessHandle Start(ProcessStartInfo startInfo)
    {
        var workingDirectory = startInfo.WorkingDirectory ?? String.Empty;
        var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start process: {startInfo.FileName} {startInfo.Arguments}");

        logger.ProcessSpawned(startInfo.FileName, startInfo.Arguments ?? String.Empty, workingDirectory);

        return new SystemProcessHandle((ILogger<ILogSystemProcessHandle>)logger, process);
    }
}
