using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Runtime;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Runtime;

public class AssemblyRuntimeDetectorTests : IDisposable
{
    private readonly Mock<IVersionsDetectorService> _versionsDetector;
    private readonly AssemblyRuntimeDetector _detector;
    private readonly string _tempDir;

    public AssemblyRuntimeDetectorTests()
    {
        _versionsDetector = new Mock<IVersionsDetectorService>();
        var logger = new Mock<ILogger<ILogAssemblyRuntimeDetector>>();
        _detector = new(logger.Object, _versionsDetector.Object);
        _tempDir = Path.Combine(Path.GetTempPath(), $"asl_wh_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    private void WriteRuntimeConfig(string assemblyPath, string frameworkVersion, string? tfm = null)
    {
        var directory = Path.GetDirectoryName(assemblyPath) ?? String.Empty;
        var configPath = Path.Combine(directory, "App.runtimeconfig.json");

        var actualTfm = tfm ?? "net8.0";

        var json =
            $$"""
            {
              "runtimeOptions": {
                "tfm": "{{actualTfm}}",
                "framework": {
                  "name": "Microsoft.AspNetCore.App",
                  "version": "{{frameworkVersion}}"
                }
              }
            }
            """;

        File.WriteAllText(configPath, json);
    }

    private void WriteTfmOnlyRuntimeConfig(string assemblyPath, string tfm)
    {
        var directory = Path.GetDirectoryName(assemblyPath) ?? String.Empty;
        var configPath = Path.Combine(directory, "App.runtimeconfig.json");

        var json =
            $$"""
            {
              "runtimeOptions": {
                "tfm": "{{tfm}}"
              }
            }
            """;

        File.WriteAllText(configPath, json);
    }

    #region Detect - Valid assemblies

    [Fact]
    public void Detect_ValidNet8Assembly_ReturnsCompatibleInfo()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "App.dll");
        File.WriteAllText(path, "fake dll");
        WriteRuntimeConfig(path, "8.0");
        _versionsDetector.Setup(v => v.IsChannelInstalled("8.0", DotNetFrameworkTypes.AspNetCore)).Returns(true);

        // Act
        var result = _detector.Detect(path);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("8.0", result.Channel);
        Assert.True(result.IsCompatible);
        Assert.Null(result.MissingMessage);
    }

    [Fact]
    public void Detect_ValidNet9Assembly_ReturnsIncompatibleInfo()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "App.dll");
        File.WriteAllText(path, "fake dll");
        WriteRuntimeConfig(path, "9.0");
        _versionsDetector.Setup(v => v.IsChannelInstalled("9.0", DotNetFrameworkTypes.AspNetCore)).Returns(false);

        // Act
        var result = _detector.Detect(path);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("9.0", result.Channel);
        Assert.False(result.IsCompatible);
        Assert.NotNull(result.MissingMessage);
    }

    [Fact]
    public void Detect_MalformedJson_ReturnsNull()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "App.dll");
        File.WriteAllText(path, "fake dll");
        var directory = Path.GetDirectoryName(path) ?? String.Empty;
        var configPath = Path.Combine(directory, "App.runtimeconfig.json");
        File.WriteAllText(configPath, "{ invalid json content");

        // Act
        var result = _detector.Detect(path);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Detect_MissingRuntimeOptionsKey_ReturnsNull()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "App.dll");
        File.WriteAllText(path, "fake dll");
        var directory = Path.GetDirectoryName(path) ?? String.Empty;
        var configPath = Path.Combine(directory, "App.runtimeconfig.json");
        File.WriteAllText(configPath, """{"otherKey": "value"}""");

        // Act
        var result = _detector.Detect(path);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Detect_TfmWithoutVersionDigits_ReturnsNull()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "App.dll");
        File.WriteAllText(path, "fake dll");
        var directory = Path.GetDirectoryName(path) ?? String.Empty;
        var configPath = Path.Combine(directory, "App.runtimeconfig.json");
        File.WriteAllText(configPath, """{"runtimeOptions": {"tfm": "net"}}""");

        // Act
        var result = _detector.Detect(path);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Detect_WithTfmFallback_ExtractsChannelFromTfm()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "App.dll");
        File.WriteAllText(path, "fake dll");
        WriteTfmOnlyRuntimeConfig(path, "net8.0");
        _versionsDetector.Setup(v => v.IsChannelInstalled("8.0", DotNetFrameworkTypes.AspNetCore)).Returns(true);

        // Act
        var result = _detector.Detect(path);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("8.0", result.Channel);
        Assert.True(result.IsCompatible);
    }

    [Fact]
    public void Detect_FullVersionInRuntimeConfig_NormalizesToChannel()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "App.dll");
        File.WriteAllText(path, "fake dll");
        WriteRuntimeConfig(path, "10.0.8");
        _versionsDetector.Setup(v => v.IsChannelInstalled("10.0", DotNetFrameworkTypes.AspNetCore)).Returns(true);

        // Act
        var result = _detector.Detect(path);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("10.0", result.Channel);
        Assert.True(result.IsCompatible);
        Assert.Null(result.MissingMessage);
    }

    #endregion

    #region Detect - Edge cases

    [Fact]
    public void Detect_NonExistentFile_ReturnsNull()
    {
        // Act
        var result = _detector.Detect("/nonexistent/path/App.dll");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Detect_NonDotNetFile_ReturnsNull()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "fake.dll");
        File.WriteAllText(path, "this is not a .NET app");

        // Act
        var result = _detector.Detect(path);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Detect_EmptyFile_ReturnsNull()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "empty.dll");
        File.WriteAllBytes(path, []);

        // Act
        var result = _detector.Detect(path);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Detect_NoRuntimeConfig_ReturnsNull()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "App.dll");
        File.WriteAllText(path, "fake dll");
        // No .runtimeconfig.json written

        // Act
        var result = _detector.Detect(path);

        // Assert
        Assert.Null(result);
    }

    #endregion

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }

        catch
        {
            // Best-effort cleanup
        }
    }
}
