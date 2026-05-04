using System.Formats.Tar;
using System.IO.Compression;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Infrastructure;

public class ArchiveExtractorServiceTests : IDisposable
{
    private readonly string _tempBase;
    private readonly string _tempExtract;

    public ArchiveExtractorServiceTests()
    {
        _tempBase = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _tempExtract = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempBase);
        Directory.CreateDirectory(_tempExtract);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempBase))
            Directory.Delete(_tempBase, true);
        if (Directory.Exists(_tempExtract))
            Directory.Delete(_tempExtract, true);
    }

    private ArchiveExtractorService CreateService()
    {
        var fileManager = new Mock<IFileManagerService>();
        fileManager.Setup(f => f.GetDirectory(String.Empty)).Returns(_tempExtract);
        fileManager.Setup(f => f.BaseDirectory).Returns(_tempBase);
        var logger = new Mock<ILogger<ArchiveExtractorService>>();
        return new(fileManager.Object, logger.Object);
    }

    #region Valid Archive Extraction

    [Fact]
    public void Decompress_ValidArchive_ExtractsFiles()
    {
        // Arrange
        var archivePath = CreateValidArchive();
        var service = CreateService();

        // Act
        service.Decompress(archivePath);

        // Assert
        var extractedFile = Path.Combine(_tempExtract, "testfile.txt");
        Assert.True(File.Exists(extractedFile));
        Assert.Equal("test content", File.ReadAllText(extractedFile));
    }

    [Fact]
    public void Decompress_ValidArchive_ExtractsDirectories()
    {
        // Arrange
        var archivePath = CreateArchiveWithDirectory();
        var service = CreateService();

        // Act
        service.Decompress(archivePath);

        // Assert
        var extractedDir = Path.Combine(_tempExtract, "subdir");
        Assert.True(Directory.Exists(extractedDir));
    }

    #endregion

    #region Zip Slip Protection

    [Fact]
    public void Decompress_ZipSlipEntry_SkipsMaliciousEntry()
    {
        // Arrange
        var archivePath = CreateArchiveWithZipSlip();
        var service = CreateService();

        // Act
        service.Decompress(archivePath);

        // Assert - malicious entry should not be extracted outside target directory
        var escapedPath = Path.GetFullPath(Path.Combine(_tempExtract, "../../escaped.txt"));
        Assert.False(File.Exists(escapedPath));
    }

    [Fact]
    public void Decompress_ZipSlipEntry_ExtractsSafeEntry()
    {
        // Arrange
        var archivePath = CreateArchiveWithZipSlip();
        var service = CreateService();

        // Act
        service.Decompress(archivePath);

        // Assert - safe entry should still be extracted
        Assert.True(File.Exists(Path.Combine(_tempExtract, "safe.txt")));
    }

    #endregion

    #region Exclusion

    [Fact]
    public void Decompress_ExcludeParameter_SkipsMatchingFile()
    {
        // Arrange
        var archivePath = CreateArchiveWithMultipleFiles();
        var service = CreateService();

        // Act
        service.Decompress(archivePath, exclude: "exclude_me.txt");

        // Assert
        Assert.True(File.Exists(Path.Combine(_tempExtract, "keep_me.txt")));
        Assert.False(File.Exists(Path.Combine(_tempExtract, "exclude_me.txt")));
    }

    [Fact]
    public void Decompress_ExcludeCaseInsensitive_SkipsMatchingFile()
    {
        // Arrange
        var archivePath = CreateArchiveWithMultipleFiles();
        var service = CreateService();

        // Act
        service.Decompress(archivePath, exclude: "EXCLUDE_ME.TXT");

        // Assert
        Assert.True(File.Exists(Path.Combine(_tempExtract, "keep_me.txt")));
        Assert.False(File.Exists(Path.Combine(_tempExtract, "exclude_me.txt")));
    }

    #endregion

    #region Error Handling

    [Fact]
    public void Decompress_NullInput_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Decompress(null!));
    }

    [Fact]
    public void Decompress_EmptyInput_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Decompress(""));
    }

    [Fact]
    public void Decompress_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => service.Decompress("/nonexistent/file.tar.gz"));
    }

    #endregion

    #region Helper Methods

    private string CreateValidArchive()
    {
        var archivePath = Path.Combine(_tempBase, "test.tar.gz");
        var tempFile = Path.Combine(_tempBase, "testfile.txt");
        File.WriteAllText(tempFile, "test content");

        using var stream = new FileStream(archivePath, FileMode.Create);
        using var gzip = new GZipStream(stream, CompressionLevel.Optimal);
        using var tar = new TarWriter(gzip);

        tar.WriteEntry(tempFile, "testfile.txt");

        File.Delete(tempFile);
        return archivePath;
    }

    private string CreateArchiveWithDirectory()
    {
        var archivePath = Path.Combine(_tempBase, "testdir.tar.gz");
        var dirPath = Path.Combine(_tempBase, "subdir");
        Directory.CreateDirectory(dirPath);

        using var stream = new FileStream(archivePath, FileMode.Create);
        using var gzip = new GZipStream(stream, CompressionLevel.Optimal);
        using var tar = new TarWriter(gzip);

        tar.WriteEntry(dirPath, "subdir");

        Directory.Delete(dirPath);
        return archivePath;
    }

    private string CreateArchiveWithZipSlip()
    {
        // Create tar.gz with path traversal entry using Python
        var archivePath = Path.Combine(_tempBase, "zipslip.tar.gz");

        // Use Python to create a tar.gz with path traversal
        var pythonScript = $@"
import tarfile, io
with tarfile.open('{archivePath}', 'w:gz') as tar:
    # Safe entry
    safe_info = tarfile.TarInfo(name='safe.txt')
    safe_data = b'safe content'
    safe_info.size = len(safe_data)
    tar.addfile(safe_info, io.BytesIO(safe_data))
    # Malicious entry with path traversal
    malicious_info = tarfile.TarInfo(name='../../escaped.txt')
    malicious_data = b'escaped content'
    malicious_info.size = len(malicious_data)
    tar.addfile(malicious_info, io.BytesIO(malicious_data))
";

        var result = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "python3",
            Arguments = $"-c \"{pythonScript}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        result?.WaitForExit();

        return archivePath;
    }

    private string CreateArchiveWithMultipleFiles()
    {
        var archivePath = Path.Combine(_tempBase, "multi.tar.gz");
        var keepFile = Path.Combine(_tempBase, "keep_me.txt");
        var excludeFile = Path.Combine(_tempBase, "exclude_me.txt");
        File.WriteAllText(keepFile, "keep");
        File.WriteAllText(excludeFile, "exclude");

        using var stream = new FileStream(archivePath, FileMode.Create);
        using var gzip = new GZipStream(stream, CompressionLevel.Optimal);
        using var tar = new TarWriter(gzip);

        tar.WriteEntry(keepFile, "keep_me.txt");
        tar.WriteEntry(excludeFile, "exclude_me.txt");

        File.Delete(keepFile);
        File.Delete(excludeFile);
        return archivePath;
    }

    #endregion
}
