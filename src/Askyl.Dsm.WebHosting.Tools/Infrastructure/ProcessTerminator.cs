using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Cross-platform utility for gracefully terminating processes.
/// Sends SIGTERM on Unix/Linux/macOS, CloseMainWindow on Windows.
/// </summary>
public static partial class ProcessTerminator
{
    // POSIX standard signal value — kept here since it's a platform constant, not an application-level configuration.
    private const int SigtermSignal = 15;

    [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static partial int SysKill(int pid, int signal);

    /// <summary>
    /// Sends a graceful termination signal to the process.
    /// Uses SIGTERM on Unix/Linux/macOS, CloseMainWindow on Windows.
    /// </summary>
    public static void SendGracefulShutdownSignal(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            process.CloseMainWindow();
        }
        else
        {
            int result = SysKill(process.Id, SigtermSignal);

            if (result != 0)
            {
                int errno = Marshal.GetLastPInvokeError();
                throw new IOException($"Failed to send SIGTERM signal to process. Error code: {errno}");
            }
        }
    }
}
