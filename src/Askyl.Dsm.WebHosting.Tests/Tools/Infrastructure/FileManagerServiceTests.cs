using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Infrastructure;

public class FileManagerServiceTests : IDisposable
{
    private readonly string _tempBase;

    public FileManagerServiceTests()
    {
        _tempBase = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempBase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempBase))
        {
            Directory.Delete(_tempBase, true);
        }
    }

    private FileManagerService CreateService(string rootPath = "")
    {
        var logger = new Mock<ILogger<FileManagerService>>();
        return new(logger.Object, rootPath);
    }

    #region SanitizePathSegment - Path Traversal

    [Fact]
    public void GetDirectory_PathTraversal_SanitizesToFilename()
    {
        // Arrange
        var service = CreateService(_tempBase);

        // Act
        var result = service.GetDirectory("../../etc/passwd");

        // Assert - path traversal is neutralized, only "passwd" remains
        Assert.Equal(Path.Combine(_tempBase, "passwd"), result);
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void GetDirectory_EmptyName_ReturnsRootDirectory()
    {
        // Arrange
        var service = CreateService(_tempBase);

        // Act
        var result = service.GetDirectory(String.Empty);

        // Assert
        Assert.Equal(_tempBase, result);
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void GetDirectory_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService(_tempBase);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.GetDirectory(null!));
    }

    [Fact]
    public void GetDirectory_WhitespaceOnly_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService(_tempBase);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.GetDirectory("   "));
    }

    [Fact]
    public void GetDirectory_SeparatorOnly_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService(_tempBase);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.GetDirectory("///"));
    }

    #endregion

    #region GetDirectory - Valid Operations

    [Fact]
    public void GetDirectory_ValidName_CreatesDirectory()
    {
        // Arrange
        var service = CreateService(_tempBase);
        var dirName = "testdir";

        // Act
        var result = service.GetDirectory(dirName);

        // Assert
        Assert.True(Directory.Exists(result));
        Assert.Equal(Path.Combine(_tempBase, dirName), result);
    }

    [Fact]
    public void GetDirectory_ExistingDirectory_ReturnsPath()
    {
        // Arrange
        var service = CreateService(_tempBase);
        var dirName = "existing";
        service.GetDirectory(dirName);

        // Act
        var result = service.GetDirectory(dirName);

        // Assert
        Assert.True(Directory.Exists(result));
        Assert.Equal(Path.Combine(_tempBase, dirName), result);
    }

    [Fact]
    public void GetDirectory_ExtractsFilenameFromPath()
    {
        // Arrange
        var service = CreateService(_tempBase);

        // Act
        var result = service.GetDirectory("parent/child");

        // Assert
        Assert.True(Directory.Exists(result));
        Assert.Equal(Path.Combine(_tempBase, "child"), result);
    }

    #endregion

    #region DeleteDirectory

    [Fact]
    public void DeleteDirectory_ExistingDirectory_DeletesDirectory()
    {
        // Arrange
        var service = CreateService(_tempBase);
        var dirName = "todelete";
        var dirPath = service.GetDirectory(dirName);

        // Act
        service.DeleteDirectory(dirName);

        // Assert
        Assert.False(Directory.Exists(dirPath));
    }

    [Fact]
    public void DeleteDirectory_NonExistingDirectory_NoOp()
    {
        // Arrange
        var service = CreateService(_tempBase);

        // Act & Assert - should not throw
        service.DeleteDirectory("nonexistent");
    }

    #endregion

    #region GetFullName

    [Fact]
    public void GetFullName_ValidInputs_ReturnsFullPath()
    {
        // Arrange
        var service = CreateService(_tempBase);
        var dirName = "mydir";
        var fileName = "myfile.txt";

        // Act
        var result = service.GetFullName(dirName, fileName);

        // Assert
        Assert.Equal(Path.Combine(_tempBase, dirName, fileName), result);
    }

    [Fact]
    public void GetFullName_PathTraversalInFile_SanitizesToFilename()
    {
        // Arrange
        var service = CreateService(_tempBase);

        // Act
        var result = service.GetFullName("dir", "../etc/passwd");

        // Assert - path traversal is neutralized, only "passwd" remains
        Assert.Equal(Path.Combine(_tempBase, "dir", "passwd"), result);
    }

    [Fact]
    public void GetFullName_EmptyFile_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService(_tempBase);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.GetFullName("dir", ""));
    }

    [Fact]
    public void GetFullName_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var service = CreateService(_tempBase);
        var dirName = "newdir";

        // Act
        service.GetFullName(dirName, "file.txt");

        // Assert
        Assert.True(Directory.Exists(Path.Combine(_tempBase, dirName)));
    }

    #endregion

    #region Initialize

    [Fact]
    public void Initialize_CreatesDefaultDirectories()
    {
        // Arrange
        var service = CreateService(_tempBase);

        // Act
        service.Initialize();

        // Assert
        var downloadsPath = Path.Combine(_tempBase, InfrastructureConstants.Downloads);
        var tempPath = Path.Combine(_tempBase, InfrastructureConstants.TempDirectory);
        Assert.True(Directory.Exists(downloadsPath), "downloads directory should exist");
        Assert.True(Directory.Exists(tempPath), "temp directory should exist");
    }

    #endregion
}
