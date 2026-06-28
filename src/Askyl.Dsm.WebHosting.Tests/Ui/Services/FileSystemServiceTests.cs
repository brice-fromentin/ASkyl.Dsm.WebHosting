using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.Domain.FileSystem;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.Acl;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.FileStation;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

/// <summary>
/// Regression tests verifying FileSystemService correctly delegates to IDsmSession.
/// Ensures the refactored session architecture (P5/P6) is working correctly.
/// </summary>
public class FileSystemServiceTests
{
    readonly FakeDsmSession _dsmSession;
    readonly Mock<ILogger<ILogFileSystemService>> _logger;
    readonly Mock<ILocalizer> _localizer;

    public FileSystemServiceTests()
    {
        _dsmSession = new FakeDsmSession();
        _logger = new Mock<ILogger<ILogFileSystemService>>();
        _localizer = new Mock<ILocalizer>();
        _localizer.Setup(l => l[LK.Error.OperationFailed]).Returns("Operation failed");
        _localizer.Setup(l => l[LK.Validation.PathTraversalDetected]).Returns("Path traversal detected");
    }

    FileSystemService CreateService()
    {
        return new FileSystemService(_dsmSession, _logger.Object, _localizer.Object);
    }

    static FileStationShareAdditional CreateShareAdditional(string realPath)
    {
        return new FileStationShareAdditional
        {
            RealPath = realPath,
            Time = new FileStationTime { ModifyTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };
    }

    static FileStationFileAdditional CreateFileAdditional(string realPath, long? size = null)
    {
        return new FileStationFileAdditional
        {
            RealPath = realPath,
            Size = size,
            Time = new FileStationTime { ModifyTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };
    }

    #region GetSharedFoldersAsync

    [Fact]
    public async Task GetSharedFoldersAsync_Success_ReturnsSharedFolders()
    {
        // Arrange
        var service = CreateService();
        var shares = new FileStationListShareData
        {
            Shares =
            [
                new FileStationShare
                {
                    Name = "test",
                    Path = "/test",
                    IsDirectory = true,
                    Additional = CreateShareAdditional("/volume1/test")
                }
            ]
        };

        _dsmSession.SetupExecuteAsync<FileStationListShareResponse>(new FileStationListShareResponse { Success = true, Data = shares });

        // Act
        var result = await service.GetSharedFoldersAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("test", result.Value[0].Name);
        Assert.Single(_dsmSession.ExecuteCalls);
    }

    [Fact]
    public async Task GetSharedFoldersAsync_ApiFailure_ReturnsFailureResult()
    {
        // Arrange
        var service = CreateService();
        _dsmSession.SetupExecuteAsync<FileStationListShareResponse>(null);

        // Act
        var result = await service.GetSharedFoldersAsync();

        // Assert
        Assert.False(result.Success);
    }

    #endregion

    #region GetDirectoryContentsAsync

    [Fact]
    public async Task GetDirectoryContentsAsync_DirectoryOnly_ReturnsDirectories()
    {
        // Arrange
        var service = CreateService();
        var files = new FileStationListData
        {
            Files =
            [
                new FileStationFile
                {
                    Name = "subdir",
                    Path = "/test/subdir",
                    IsDirectory = true,
                    Additional = CreateFileAdditional("/volume1/test/subdir")
                }
            ]
        };

        _dsmSession.SetupExecuteAsync<FileStationListResponse>(new FileStationListResponse { Success = true, Data = files });

        // Act
        var result = await service.GetDirectoryContentsAsync("/test", directoryOnly: true);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("subdir", result.Value[0].Name);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_WithFiles_MakesParallelCalls()
    {
        // Arrange
        var service = CreateService();
        var dirs = new FileStationListData
        {
            Files =
            [
                new FileStationFile
                {
                    Name = "subdir",
                    Path = "/test/subdir",
                    IsDirectory = true,
                    Additional = CreateFileAdditional("/volume1/test/subdir")
                }
            ]
        };
        var fileFiles = new FileStationListData
        {
            Files =
            [
                new FileStationFile
                {
                    Name = "app.dll",
                    Path = "/test/app.dll",
                    IsDirectory = false,
                    Additional = CreateFileAdditional("/volume1/test/app.dll", size: 1024)
                }
            ]
        };

        _dsmSession.SetupExecuteSequenceAsync<FileStationListResponse>(
        [
            new FileStationListResponse { Success = true, Data = dirs },
            new FileStationListResponse { Success = true, Data = dirs },
            new FileStationListResponse { Success = true, Data = fileFiles },
        ]);

        // Act
        var result = await service.GetDirectoryContentsAsync("/test", directoryOnly: false);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_PathTraversal_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetDirectoryContentsAsync("/test/../etc", directoryOnly: true);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(_dsmSession.ExecuteCalls);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_EncodedPathTraversal_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetDirectoryContentsAsync("/test/%2e%2e/etc", directoryOnly: true);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(_dsmSession.ExecuteCalls);
    }

    #endregion

    #region SetHttpGroupPermissionsAsync

    [Fact]
    public async Task SetHttpGroupPermissionsAsync_Success_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();
        var aclResponse = new CoreAclSetResponse
        {
            Success = true,
            Data = new CoreAclSetData { TaskId = "task-123" }
        };

        _dsmSession.SetupExecuteAsync<CoreAclSetResponse>(aclResponse);

        // Act
        var result = await service.SetHttpGroupPermissionsAsync("/volume1/test/app.dll", isDirectory: false);

        // Assert
        Assert.True(result.Success);
        Assert.Single(_dsmSession.ExecuteCalls);
    }

    [Fact]
    public async Task SetHttpGroupPermissionsAsync_PathTraversal_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SetHttpGroupPermissionsAsync("/volume1/../../etc/passwd", isDirectory: false);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(_dsmSession.ExecuteCalls);
    }

    #endregion

    #region FakeDsmSession

    /// <summary>
    /// Fake implementation of IDsmSession that avoids Moq's generic method handling issues.
    /// </summary>
    sealed class FakeDsmSession : IDsmSession
    {
        public string? UserLanguage { get; set; }
        public string? UserDateFormat { get; set; }
        public string? UserTimeFormat { get; set; }

#pragma warning disable IDE0028 // Collection initialization can be simplified
        readonly Dictionary<string, object?> _responses = new();
        readonly Dictionary<string, Queue<object?>> _sequences = new();
#pragma warning restore IDE0028
        public List<string> ExecuteCalls { get; } = [];

        public void SetupExecuteAsync<T>(T? response) where T : IApiResponse
        {
            _responses[typeof(T).Name] = response;
        }

        public void SetupExecuteSequenceAsync<T>(IEnumerable<T?> responses) where T : IApiResponse
        {
            _sequences[typeof(T).Name] = new Queue<object?>(responses.Cast<object?>());
        }

        public Task<bool> ConnectAsync(Askyl.Dsm.WebHosting.Data.Domain.Authentication.LoginCredentials model, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> ValidateSessionAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void Disconnect() { }

        public Task<R?> ExecuteAsync<R>(IApiParameters parameters, CancellationToken cancellationToken = default) where R : IApiResponse
        {
            var allowedNames = new[] { "SYNO.FileStation.List", "SYNO.Core.ACL" };

            if (!allowedNames.Contains(parameters.Name))
            {
                throw new ArgumentException($"Unexpected API name: {parameters.Name}", nameof(parameters));
            }

            var key = typeof(R).Name;
            ExecuteCalls.Add($"{parameters.Name}/{parameters.Method}");

            if (_sequences.TryGetValue(key, out var queue) && queue!.Count > 0)
            {
                return Task.FromResult((R?)queue!.Dequeue());
            }

            if (_responses.TryGetValue(key, out var response))
            {
                return Task.FromResult((R?)response);
            }

            return Task.FromResult<R?>(default!);
        }

        public Task<ApiResponseBase<object>?> ExecuteSimpleAsync(IApiParameters parameters, CancellationToken cancellationToken = default)
        {
            ExecuteCalls.Add("ApiResponseBase<object>");
            return Task.FromResult<ApiResponseBase<object>?>(null);
        }
    }

    #endregion
}
