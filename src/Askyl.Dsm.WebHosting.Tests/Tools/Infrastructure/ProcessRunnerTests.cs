using System.Diagnostics;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Infrastructure;

public class ProcessRunnerTests
{
    /// <summary>
    /// Bytes the child writes to each redirected stream. Must exceed the operating system pipe
    /// buffer (64 KB on Linux) for the test to exercise the blocking condition at all.
    /// </summary>
    const int LineCount = 4000;

    const string Line = "0123456789012345678901234567890123456789";

    [Fact]
    public async Task Start_DrainsRedirectedOutput_WhenChildWritesPastPipeBuffer()
    {
        // A redirected pipe nobody reads fills the OS buffer and blocks the child on its next
        // write, forever. The child below writes ~160 KB to each stream, so it can only reach
        // exit if the runner is draining both.
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var runner = new SystemProcessRunner(NullLogger<Logging.ILogSystemProcessRunner>.Instance, NullLoggerFactory.Instance);

        using var handle = runner.Start(CreateNoisyChildStartInfo());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        await handle.WaitForExitAsync(timeout.Token);

        Assert.True(handle.HasExited);
    }

    /// <summary>
    /// Builds a POSIX shell child that floods both stdout and stderr, avoiding coreutils so the
    /// test does not depend on which utilities the host provides.
    /// </summary>
    static ProcessStartInfo CreateNoisyChildStartInfo()
    {
        var script = $"i=0; while [ $i -lt {LineCount} ]; do echo {Line}; echo {Line} >&2; i=$((i+1)); done";

        return new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = $"-c \"{script}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }
}
