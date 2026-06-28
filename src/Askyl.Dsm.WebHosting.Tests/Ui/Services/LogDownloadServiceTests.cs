using System.IO.Compression;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class LogDownloadServiceTests
{
    readonly Mock<ILogger<ILogLogDownloadService>> _logger;
    readonly Mock<IFileReader> _fileReader;

    public LogDownloadServiceTests()
    {
        _logger = new Mock<ILogger<ILogLogDownloadService>>();
        _fileReader = new Mock<IFileReader>();
    }

    LogDownloadService CreateService()
        => new(_logger.Object, _fileReader.Object);

    #region Archive Creation - No Files

    [Fact]
    public async Task CreateLogZipStreamAsync_NoFiles_ReturnsValidEmptyZip()
    {
        // Arrange
        _fileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileReader.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);

        // Act
        using var stream = await CreateService().CreateLogZipStreamAsync();

        // Assert
        Assert.IsType<MemoryStream>(stream);
        Assert.True(stream.Length > 0, "ZIP archive should not be empty");

        stream.Position = 0;
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.NotNull(archive);
    }

    #endregion

    #region Archive Creation - With Debug Log

    [Fact]
    public async Task CreateLogZipStreamAsync_WithDebugLog_ContainsDebugEntry()
    {
        // Arrange
        var debugContent = "debug log content";
        var debugPath = "/tmp/adwh-debug.log";

        _fileReader.Setup(f => f.FileExists(debugPath)).Returns(true);
        _fileReader.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);
        _fileReader.Setup(f => f.OpenRead(debugPath)).Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(debugContent)));

        // Act
        using var stream = await CreateService().CreateLogZipStreamAsync();

        // Assert
        stream.Position = 0;
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var debugEntry = archive.Entries.FirstOrDefault(e => e.Name.Contains("debug"));
        Assert.NotNull(debugEntry);

        using var entryStream = debugEntry.Open();
        using var reader = new StreamReader(entryStream);
        var content = await reader.ReadToEndAsync();
        Assert.Equal(debugContent, content);
    }

    #endregion

    #region Archive Creation - With Directory Logs

    [Fact]
    public async Task CreateLogZipStreamAsync_WithLogDirectory_ContainsDirectoryEntries()
    {
        // Arrange
        var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        var logFile1 = Path.Combine(logDir, "app.log");
        var logFile2 = Path.Combine(logDir, "sub", "nested.log");

        _fileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileReader.Setup(f => f.DirectoryExists(logDir)).Returns(true);
        _fileReader.Setup(f => f.DirectoryExists(It.Is<string>(p => p != logDir))).Returns(false);
        _fileReader.Setup(f => f.EnumerateFiles(logDir, "*", true))
                   .Returns(new[] { logFile1, logFile2 });

        _fileReader.Setup(f => f.OpenRead(logFile1)).Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("log file 1")));
        _fileReader.Setup(f => f.OpenRead(logFile2)).Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("log file 2")));

        // Act
        using var stream = await CreateService().CreateLogZipStreamAsync();

        // Assert
        stream.Position = 0;
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.Equal(2, archive.Entries.Count);
    }

    #endregion

    #region Stream Position Reset

    [Fact]
    public async Task CreateLogZipStreamAsync_ReturnsStreamAtPositionZero()
    {
        // Arrange
        _fileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileReader.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);

        // Act
        using var stream = await CreateService().CreateLogZipStreamAsync();

        // Assert
        Assert.Equal(0, stream.Position);
    }

    #endregion

    #region Multiple Invocations

    [Fact]
    public async Task CreateLogZipStreamAsync_MultipleCalls_ReturnsIndependentStreams()
    {
        // Arrange
        _fileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileReader.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);
        var service = CreateService();

        // Act
        using var stream1 = await service.CreateLogZipStreamAsync();
        using var stream2 = await service.CreateLogZipStreamAsync();

        // Assert
        Assert.NotSame(stream1, stream2);
        Assert.True(stream1.Length > 0);
        Assert.True(stream2.Length > 0);
    }

    #endregion
}
