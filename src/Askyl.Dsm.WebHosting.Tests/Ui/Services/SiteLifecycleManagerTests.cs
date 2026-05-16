using System.Diagnostics;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
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
    private readonly FakeProcessRunner _processRunner;
    private readonly FakeProcessHandle _processHandle;
    private readonly WebSiteConfiguration _configuration;
    private readonly List<ProcessStartInfo> _startedProcesses;
    private readonly string _tempDir;
    private readonly string _tempDll;

    public SiteLifecycleManagerTests()
    {
        _logger = new Mock<ILogger<ILogSiteLifecycleManager>>();
        _processRunner = new FakeProcessRunner();
        _processHandle = new FakeProcessHandle();
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
        return new SiteLifecycleManager(_logger.Object, _processRunner, _configuration);
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
        _processHandle.SimulateRunning = true;
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
        _processHandle.SimulateRunning = true;
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
        _processHandle.SimulateRunning = true;
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
        _processHandle.SimulateRunning = true;
        var manager = CreateManager();

        await manager.StartAsync();

        // Act
        manager.Dispose();

        // Wait for background dispose command to process
        await Task.Delay(100);

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

            return HandleToReturn
                ?? throw new InvalidOperationException("HandleToReturn not set");
        }
    }

    private sealed class FakeProcessHandle : IProcessHandle
    {
        public bool SimulateRunning;
        public int FakeId = 9999;

        public bool GracefulShutdownCalled { get; private set; }
        public bool WaitForExitCalled { get; private set; }
        public bool KillCalled { get; private set; }

        public int Id => FakeId;
        public bool HasExited => !SimulateRunning;

        public void SendGracefulShutdownSignal()
        {
            GracefulShutdownCalled = true;
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken)
        {
            WaitForExitCalled = true;
            SimulateRunning = false;

            return Task.CompletedTask;
        }

        public void Kill()
        {
            KillCalled = true;
            SimulateRunning = false;
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
}
