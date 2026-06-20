using System.IO.Compression;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class LogDownloadServiceTests
{
    private LogDownloadService CreateService()
    {
        var logger = new Mock<ILogger<ILogLogDownloadService>>();
        return new(logger.Object);
    }

    #region Archive Creation

    [Fact]
    public async Task CreateLogZipStreamAsync_ReturnsValidZipArchive()
    {
        // Act
        using var stream = await CreateService().CreateLogZipStreamAsync();

        // Assert
        Assert.IsType<MemoryStream>(stream);
        Assert.True(stream.Length > 0, "ZIP archive should not be empty");

        // Verify it's a valid ZIP by opening it
        stream.Position = 0;
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.NotNull(archive);
    }

    #endregion

    #region Stream Position Reset

    [Fact]
    public async Task CreateLogZipStreamAsync_ReturnsStreamAtPositionZero()
    {
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
