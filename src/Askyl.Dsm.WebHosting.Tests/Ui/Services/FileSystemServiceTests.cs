using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.Acl;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.Acl;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.FileStation;
using Askyl.Dsm.WebHosting.Data.Results;
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

    [Fact]
    public async Task GetDirectoryContentsAsync_DoubleEncodedPathTraversal_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetDirectoryContentsAsync("/volume1/%252e%252e/etc/passwd", directoryOnly: true);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(_dsmSession.ExecuteCalls);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_MixedEncodingPathTraversal_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetDirectoryContentsAsync("/volume1/%2e%2e%252f..%252fetc", directoryOnly: true);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(_dsmSession.ExecuteCalls);
    }

    #endregion

    #region GrantSiteAccessAsync

    static CoreAclRule Entry(string owner, int level, bool readData = true) => new()
    {
        OwnerType = AclConstants.OwnerTypeGroup,
        OwnerName = owner,
        OwnerId = 101,
        PermissionType = AclConstants.PermissionTypeAllow,
        Permission = new() { ReadData = readData, ExeFile = true },
        Inherit = new() { ThisFolder = true, ChildFiles = true, ChildFolders = true, AllDescendants = true },
        Level = level
    };

    static CoreAclGetResponse Acl(bool isInherited, params CoreAclRule[] entries)
        => new() { Success = true, Data = new() { IsInherited = isInherited, Acl = [.. entries] } };

    void SetupAcls(params CoreAclGetResponse[] folders)
    {
        _dsmSession.SetupExecuteSequenceAsync(folders);
        _dsmSession.SetupExecuteAsync(new CoreAclSetResponse { Success = true, Data = new() { TaskId = "task" } });
        _dsmSession.SetupExecuteAsync(new CoreAclStatusResponse { Success = true, Data = new() { Finished = true } });
        _localizer.Setup(l => l[LK.Error.SiteAccessGrantFailed, It.IsAny<object>()]).Returns("Grant failed");
    }

    List<CoreAclSet> SetCalls() => [.. _dsmSession.Parameters.OfType<CoreAclSetParameters>().Select(p => p.Parameters)];

    [Fact]
    public async Task GrantSiteAccessAsync_PassesThroughEachFolderAbove_ReadsTheSiteFolder_AndKeepsTheirEntries()
    {
        // Arrange — the shared folder, a folder in between, the site's folder: as measured on the Virtual DSM
        var service = CreateService();
        var administrators = Entry("administrators", 0);
        var someone = Entry("someone", 0, readData: false);
        SetupAcls(
            Acl(isInherited: false, administrators),
            Acl(isInherited: true, administrators with { Level = 1 }),
            Acl(isInherited: true, administrators with { Level = 2 }, someone));

        // Act
        var result = await service.GrantSiteAccessAsync(new("/data/websites/blog/Blog.dll", "/volume1/data/websites/blog/Blog.dll"));

        // Assert — set replaces the explicit entries, so each one goes back; inherited ones are DSM's to keep
        Assert.True(result.Success);
        var sets = SetCalls();
        Assert.Equal(["/data", "/data/websites", "/data/websites/blog"], sets.Select(s => s.DirPaths));
        Assert.Equal(["/volume1/data", "/volume1/data/websites", "/volume1/data/websites/blog"], sets.Select(s => s.FilePath));
        Assert.Equal([false, true, true], sets.Select(s => s.Inherited));
        Assert.Equal(["administrators", "http"], sets[0].Rules.Select(r => r.OwnerName));
        Assert.Equal(["http"], sets[1].Rules.Select(r => r.OwnerName));
        Assert.Equal(["someone", "http"], sets[2].Rules.Select(r => r.OwnerName));
        Assert.All(sets.SelectMany(s => s.Rules), r => Assert.Null(r.Level));

        // Folders above: passage only, on that folder only. The site's folder: Read, inherited by everything in it
        var traverse = sets[0].Rules[^1];
        Assert.Equal(new CoreAclPermission { ExeFile = true }, traverse.Permission);
        Assert.Equal(new CoreAclInherit { ThisFolder = true }, traverse.Inherit);
        Assert.Equal(traverse, sets[1].Rules[^1]);
        var read = sets[2].Rules[^1];
        Assert.Equal(new CoreAclPermission { ReadData = true, ExeFile = true, ReadAttr = true, ReadExtAttr = true, ReadPerm = true }, read.Permission);
        Assert.Equal(new CoreAclInherit { ThisFolder = true, ChildFiles = true, ChildFolders = true, AllDescendants = true }, read.Inherit);
    }

    [Fact]
    public async Task GrantSiteAccessAsync_SendsEveryPermissionKey_AndOwnerIdButNotLevel()
    {
        // DSM refused a rule whose permission object lacked keys (403), and its own editor sends owner_id back
        var service = CreateService();
        SetupAcls(Acl(isInherited: false, Entry("administrators", 0)), Acl(isInherited: true));

        await service.GrantSiteAccessAsync(new("/data/blog/Blog.dll", "/volume1/data/blog/Blog.dll"));

        var form = Uri.UnescapeDataString(await _dsmSession.Parameters.OfType<CoreAclSetParameters>().First().ToForm().ReadAsStringAsync());
        Assert.Contains("\"owner_id\":101", form, StringComparison.Ordinal);
        Assert.Contains("\"write_data\":false", form, StringComparison.Ordinal);
        Assert.DoesNotContain("\"level\"", form, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GrantSiteAccessAsync_LeavesAFolderWithItsOwnHttpEntryAlone()
    {
        // An http entry someone set is theirs: replacing it could lift a deny or take away what they gave
        var service = CreateService();
        SetupAcls(Acl(isInherited: false, Entry("administrators", 0), Entry(AclConstants.OwnerNameHttp, 0)), Acl(isInherited: true));

        var result = await service.GrantSiteAccessAsync(new("/data/blog/Blog.dll", "/volume1/data/blog/Blog.dll"));

        Assert.True(result.Success);
        Assert.Equal(["/data/blog"], SetCalls().Select(s => s.DirPaths));
    }

    [Fact]
    public async Task GrantSiteAccessAsync_StopsAtTheFirstRefusal()
    {
        var service = CreateService();
        SetupAcls(Acl(isInherited: false), Acl(isInherited: true));
        _dsmSession.SetupExecuteAsync(new CoreAclSetResponse { Success = false });

        var result = await service.GrantSiteAccessAsync(new("/data/blog/Blog.dll", "/volume1/data/blog/Blog.dll"));

        Assert.False(result.Success);
        Assert.Equal("Grant failed", result.Message);
        Assert.Single(SetCalls());
    }

    [Theory]
    [InlineData("/data/blog/Blog.dll", "/volume1/other/blog/Blog.dll")]
    [InlineData("/data/../etc/Blog.dll", "/volume1/data/../etc/Blog.dll")]
    [InlineData("/data/%252e%252e/Blog.dll", "/volume1/data/%252e%252e/Blog.dll")]
    [InlineData("data/blog/Blog.dll", "/volume1/data/blog/Blog.dll")]
    [InlineData("/Blog.dll", "/volume1/Blog.dll")]
    public async Task GrantSiteAccessAsync_RefusesPathsItCannotTrust_WithoutCallingDsm(string applicationPath, string realPath)
    {
        // Both paths come from the browser: the share path must be the real path's tail, inside a shared folder
        var service = CreateService();

        var result = await service.GrantSiteAccessAsync(new(applicationPath, realPath));

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
        public List<IApiParameters> Parameters { get; } = [];

        public void SetupExecuteAsync<T>(T? response) where T : IApiResponse
        {
            _responses[typeof(T).Name] = response;
        }

        public void SetupExecuteSequenceAsync<T>(IEnumerable<T?> responses) where T : IApiResponse
        {
            _sequences[typeof(T).Name] = new Queue<object?>(responses.Cast<object?>());
        }

        public Task<ApiResult> ConnectAsync(Askyl.Dsm.WebHosting.Data.Domain.Authentication.LoginCredentials model, CancellationToken cancellationToken = default) => Task.FromResult(ApiResult.CreateSuccess());
        public Task<bool> ValidateSessionAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public bool HasSession => true;
        public void Disconnect() { }
        public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<R?> ExecuteAsync<R>(IApiParameters parameters, CancellationToken cancellationToken = default) where R : IApiResponse
        {
            var allowedNames = new[] { "SYNO.FileStation.List", "SYNO.Core.ACL" };

            if (!allowedNames.Contains(parameters.Name))
            {
                throw new ArgumentException($"Unexpected API name: {parameters.Name}", nameof(parameters));
            }

            var key = typeof(R).Name;
            ExecuteCalls.Add($"{parameters.Name}/{parameters.Method}");
            Parameters.Add(parameters);

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
