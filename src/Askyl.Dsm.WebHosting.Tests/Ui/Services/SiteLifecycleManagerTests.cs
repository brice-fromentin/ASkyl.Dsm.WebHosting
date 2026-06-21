using System.Diagnostics;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

/// <summary>
/// Tests for SiteLifecycleManager using fake IProcessRunner/IProcessHandle.
/// Validates the IProcessRunner abstraction enables testable process lifecycle management.
/// </summary>
public class SiteLifecycleManagerTests : IDisposable
{
    private readonly Mock<ILogger<ILogSiteLifecycleManager>> _logger;
    private readonly Mock<ILocalizer> _localizer;
    private readonly FakeProcessRunner _processRunner;
    private readonly FakeProcessHandle _processHandle;
    private readonly Mock<IAssemblyRuntimeDetector> _detector;
    private readonly WebSiteConfiguration _configuration;
    private readonly List<ProcessStartInfo> _startedProcesses;
    private readonly string _tempDir;
    private readonly string _tempDll;

    public SiteLifecycleManagerTests()
    {
        _logger = new Mock<ILogger<ILogSiteLifecycleManager>>();
        _localizer = new Mock<ILocalizer>();
        _processRunner = new FakeProcessRunner();
        _processHandle = new FakeProcessHandle();
        _detector = new Mock<IAssemblyRuntimeDetector>();
        _startedProcesses = _processRunner.StartedProcesses;
        _tempDir = Path.Combine(Path.GetTempPath(), $"asl_wh_{Guid.NewGuid():N}");
        _tempDll = Path.Combine(_tempDir, "MyApp.dll");

        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_tempDll, "");

        _configuration = new WebSiteConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "TestSite",
            ApplicationPath = _tempDir,
            ApplicationRealPath = _tempDll,
            InternalPort = 5001,
            HostName = "test.local",
            Environment = "Production",
            ProcessTimeoutSeconds = 1,
            AdditionalEnvironmentVariables = new Dictionary<string, string> { ["CUSTOM_VAR"] = "test" }
        };

        _processRunner.HandleToReturn = _processHandle;
    }

    private SiteLifecycleManager CreateManager()
    {
        return new SiteLifecycleManager(_logger.Object, _localizer.Object, _processRunner, _detector.Object, _configuration);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best-effort
        }
    }

    #region StartAsync

    [Fact]
    public async Task StartAsync_WhenStopped_StartsProcessViaRunner()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Single(_startedProcesses);

        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_ConfiguresProcessStartInfoCorrectly()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var result = await manager.StartAsync();
        Assert.True(result.Success);

        // Assert
        var startInfo = _startedProcesses.Single();
        Assert.Equal(WebSiteConstants.DotnetExecutable, startInfo.FileName);
        Assert.Equal(_configuration.ApplicationRealPath, startInfo.Arguments);
        Assert.Equal(Path.GetDirectoryName(_configuration.ApplicationRealPath), startInfo.WorkingDirectory);
        Assert.False(startInfo.UseShellExecute);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
        Assert.True(startInfo.CreateNoWindow);
        Assert.Equal($"http://localhost:{_configuration.InternalPort}", startInfo.Environment[WebSiteConstants.AspNetCoreUrlsEnvironmentVariable]);
        Assert.Equal(_configuration.Environment, startInfo.Environment[WebSiteConstants.AspNetCoreEnvironmentVariable]);
        Assert.Equal("test", startInfo.Environment["CUSTOM_VAR"]);

        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_ApplicationNotFound_ReturnsFailure()
    {
        // Arrange
        _configuration.ApplicationRealPath = "/nonexistent/path/app.dll";
        var manager = CreateManager();

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Empty(_startedProcesses);

        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_RunnerThrows_ReturnsFailure()
    {
        // Arrange
        _processRunner.ShouldThrow = true;
        var manager = CreateManager();

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.False(result.Success);

        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ReturnsFailure()
    {
        // Arrange
        var manager = CreateManager();

        await manager.StartAsync();

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.False(result.Success);

        manager.Dispose();
    }

    #endregion

    #region StopAsync

    [Fact]
    public async Task StopAsync_WhenStopped_ReturnsSuccess()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var result = await manager.StopAsync();

        // Assert
        Assert.True(result.Success);

        manager.Dispose();
    }

    [Fact]
    public async Task StopAsync_WhenRunning_SendsGracefulShutdownAndWaits()
    {
        // Arrange
        var manager = CreateManager();

        await manager.StartAsync();

        // Act
        var result = await manager.StopAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(_processHandle.GracefulShutdownCalled);
        Assert.True(_processHandle.WaitForExitCalled);

        manager.Dispose();
    }

    [Fact]
    public async Task StopAsync_ProcessDoesNotExit_ForceKills()
    {
        // Arrange
        var stubbornHandle = new FakeStubbornProcessHandle();
        _processRunner.HandleToReturn = stubbornHandle;
        var manager = CreateManager();

        await manager.StartAsync();

        // Act
        var result = await manager.StopAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(stubbornHandle.GracefulShutdownCalled);
        Assert.True(stubbornHandle.KillCalled);

        manager.Dispose();
    }

    #endregion

    #region GetRuntimeStateAsync

    [Fact]
    public async Task GetRuntimeStateAsync_WhenStopped_ReturnsStopped()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var state = await manager.GetRuntimeStateAsync();

        // Assert
        Assert.False(state.IsRunning);

        manager.Dispose();
    }

    [Fact]
    public async Task GetRuntimeStateAsync_WhenRunning_ReturnsRunningWithProcessInfo()
    {
        // Arrange
        var manager = CreateManager();

        await manager.StartAsync();

        // Act
        var state = await manager.GetRuntimeStateAsync();

        // Assert
        Assert.True(state.IsRunning);
        Assert.NotNull(state.ProcessDetails);
        Assert.Equal(9999, state.ProcessDetails!.Id);

        manager.Dispose();
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_WhenStopped_CleansUpWithoutError()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.Dispose();
    }

    [Fact]
    public async Task Dispose_WhenRunning_ForceKillsProcess()
    {
        // Arrange
        var manager = CreateManager();

        await manager.StartAsync();

        // Act
        manager.Dispose();

        // Wait for background dispose command to process
        await _processHandle.KillCompleted;

        // Assert
        Assert.True(_processHandle.KillCalled);
        Assert.False(_processHandle.GracefulShutdownCalled);
    }

    [Fact]
    public void Dispose_CalledTwice_IsIdempotent()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.Dispose();
        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_AfterDispose_ReturnsFailure()
    {
        // Arrange
        var manager = CreateManager();
        manager.Dispose();

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task StopAsync_AfterDispose_ReturnsFailure()
    {
        // Arrange
        var manager = CreateManager();
        manager.Dispose();

        // Act
        var result = await manager.StopAsync();

        // Assert
        Assert.False(result.Success);
    }

    #endregion

    #region Fake Implementations

    private sealed class FakeProcessRunner : IProcessRunner
    {
        public List<ProcessStartInfo> StartedProcesses { get; } = [];
        public IProcessHandle? HandleToReturn;
        public bool ShouldThrow;

        public IProcessHandle Start(ProcessStartInfo startInfo)
        {
            StartedProcesses.Add(startInfo);

            if (ShouldThrow)
            {
                throw new InvalidOperationException("Permission denied");
            }

            var handle = HandleToReturn ?? throw new InvalidOperationException("HandleToReturn not set");
            (handle as FakeProcessHandle)?.SetRunning(true);
            return handle;
        }
    }

    private sealed class FakeProcessHandle : IProcessHandle
    {
        public int FakeId = 9999;
        private bool _isRunning;

        public bool GracefulShutdownCalled { get; private set; }
        public bool WaitForExitCalled { get; private set; }
        public bool KillCalled { get; private set; }
        public Task KillCompleted => _killTcs.Task;
        private readonly TaskCompletionSource<bool> _killTcs = new();

        public int Id => FakeId;
        public bool HasExited => !_isRunning;

        internal void SetRunning(bool running)
        {
            _isRunning = running;
        }

        public void SendGracefulShutdownSignal()
        {
            GracefulShutdownCalled = true;
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken)
        {
            WaitForExitCalled = true;
            _isRunning = false;

            return Task.CompletedTask;
        }

        public void Kill()
        {
            KillCalled = true;
            _isRunning = false;
            _killTcs.TrySetResult(true);
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Process handle that never exits gracefully, used to test the force-kill fallback path.
    /// WaitForExitAsync always throws OperationCanceledException to simulate timeout.
    /// </summary>
    private sealed class FakeStubbornProcessHandle : IProcessHandle
    {
        private bool _hasExited;
        public int FakeId = 9998;

        public bool GracefulShutdownCalled { get; private set; }
        public bool WaitForExitCalled { get; private set; }
        public bool KillCalled { get; private set; }

        public int Id => FakeId;
        public bool HasExited => _hasExited;

        public void SendGracefulShutdownSignal()
        {
            GracefulShutdownCalled = true;
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken)
        {
            WaitForExitCalled = true;

            // Simulate timeout - process never exits gracefully
            throw new OperationCanceledException();
        }

        public void Kill()
        {
            KillCalled = true;
            _hasExited = true;
        }

        public void Dispose()
        {
        }
    }

    #endregion

    #region Framework detection

    [Fact]
    public async Task StartAsync_IncompatibleFramework_ReturnsFailure()
    {
        // Arrange
        _detector.Setup(d => d.Detect(_configuration.ApplicationRealPath))
            .Returns(new AssemblyRuntimeInfo(
                "9.0", false, "Requires .NET 9.0, but this runtime is not installed"));
        var manager = CreateManager();

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Requires .NET 9.0", result.Message);
        Assert.Empty(_startedProcesses);

        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_CompatibleFramework_StartsSuccessfully()
    {
        // Arrange
        _detector.Setup(d => d.Detect(_configuration.ApplicationRealPath))
            .Returns(new AssemblyRuntimeInfo("8.0", true, null));
        var manager = CreateManager();

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Single(_startedProcesses);

        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_DetectionFallsThrough_AllowsStart()
    {
        // Arrange - detection returns null (non-.NET file)
        _detector.Setup(d => d.Detect(_configuration.ApplicationRealPath)).Returns((AssemblyRuntimeInfo?)null);
        var manager = CreateManager();

        // Act
        var result = await manager.StartAsync();

        // Assert - should still start (no blocking)
        Assert.True(result.Success);
        Assert.Single(_startedProcesses);

        manager.Dispose();
    }

    #endregion

    #region Concurrent execution

    [Fact]
    public async Task ConcurrentStartCalls_ExecuteSequentiallyWithoutRaces()
    {
        // Arrange
        _detector.Setup(d => d.Detect(_configuration.ApplicationRealPath)).Returns((AssemblyRuntimeInfo?)null);
        var manager = CreateManager();

        // Act - fire 5 concurrent start calls
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => manager.StartAsync())
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - first call succeeds, rest fail (already running)
        Assert.Single(results, r => r.Success);
        Assert.Equal(4, results.Count(r => !r.Success));

        manager.Dispose();
    }

    [Fact]
    public async Task ConcurrentStartAndStop_QueueCommandsCorrectly()
    {
        // Arrange
        _detector.Setup(d => d.Detect(_configuration.ApplicationRealPath)).Returns((AssemblyRuntimeInfo?)null);
        var manager = CreateManager();

        // Act - interleave start/stop concurrently
        var tasks = new[]
        {
            manager.StartAsync(),
            manager.StartAsync(),
            manager.StopAsync(),
            manager.StartAsync(),
        };

        var results = await Task.WhenAll(tasks);

        // Assert - at least one succeeded (channel serializes them)
        Assert.Contains(results, r => r.Success);

        manager.Dispose();
    }

    [Fact]
    public async Task StartStopStartStop_Sequence_ExecutesWithoutStateLeakage()
    {
        // Arrange
        _detector.Setup(d => d.Detect(_configuration.ApplicationRealPath)).Returns((AssemblyRuntimeInfo?)null);
        var manager = CreateManager();

        // Act - first lifecycle cycle
        var start1 = await manager.StartAsync();
        var stop1 = await manager.StopAsync();

        // Act - second lifecycle cycle
        var start2 = await manager.StartAsync();
        var stop2 = await manager.StopAsync();

        // Assert - all succeed, process started twice
        Assert.True(start1.Success);
        Assert.True(stop1.Success);
        Assert.True(start2.Success);
        Assert.True(stop2.Success);
        Assert.Equal(2, _startedProcesses.Count);

        manager.Dispose();
    }

    #endregion
}
