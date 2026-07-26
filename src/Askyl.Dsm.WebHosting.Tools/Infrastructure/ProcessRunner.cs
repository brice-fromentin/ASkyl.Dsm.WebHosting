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
/// <param name="loggerFactory">Logger factory for creating child loggers.</param>
public sealed class SystemProcessRunner(ILogger<ILogSystemProcessRunner> logger, ILoggerFactory loggerFactory) : IProcessRunner
{
    public IProcessHandle Start(ProcessStartInfo startInfo)
    {
        var workingDirectory = startInfo.WorkingDirectory ?? String.Empty;
        var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start process: {startInfo.FileName} {startInfo.Arguments}");

        logger.ProcessSpawned(startInfo.FileName, startInfo.Arguments ?? String.Empty, workingDirectory);

        DrainRedirectedStreams(process);

        return new SystemProcessHandle(loggerFactory.CreateLogger<ILogSystemProcessHandle>(), process);
    }

    /// <summary>
    /// Forwards a child's redirected output to the log. A redirected pipe that nobody reads fills the
    /// operating system buffer (64 KB on Linux) and blocks the child on its next write, so every stream
    /// enabled in <see cref="ProcessStartInfo"/> must be consumed for the lifetime of the process.
    /// </summary>
    /// <param name="process">The freshly started process.</param>
    private void DrainRedirectedStreams(Process process)
    {
        // Captured up front: the handlers outlive SystemProcessHandle.Dispose, which disposes the
        // Process, and reading Id from a disposed Process throws.
        var processId = process.Id;

        if (process.StartInfo.RedirectStandardOutput)
        {
            process.OutputDataReceived += (_, args) => LogLine(processId, args.Data, isError: false);
            process.BeginOutputReadLine();
        }

        if (process.StartInfo.RedirectStandardError)
        {
            process.ErrorDataReceived += (_, args) => LogLine(processId, args.Data, isError: true);
            process.BeginErrorReadLine();
        }
    }

    /// <summary>
    /// Logs a single output line, ignoring the null line that signals end of stream.
    /// </summary>
    /// <param name="processId">The process identifier.</param>
    /// <param name="line">The line received, or null at end of stream.</param>
    /// <param name="isError">Whether the line came from standard error.</param>
    private void LogLine(int processId, string? line, bool isError)
    {
        if (line is null)
        {
            return;
        }

        if (isError)
        {
            logger.ProcessErrorReceived(processId, line);
        }
        else
        {
            logger.ProcessOutputReceived(processId, line);
        }
    }
}
