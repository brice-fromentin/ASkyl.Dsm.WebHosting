using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Tools.Runtime;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Runtime;

public class VersionsDetectorServiceTests
{
    private readonly VersionsDetectorService _service;

    public VersionsDetectorServiceTests()
    {
        var logger = new Mock<ILogger<VersionsDetectorService>>();
        _service = new(logger.Object);
    }

    #region ParseDotnetInfo - Section Detection

    [Fact]
    public void ParseDotnetInfo_DetectsSdkSection()
    {
        // Arrange
        var output = $@"{DotnetInfoParserConstants.SdkSectionHeader}
  9.0.300 [/usr/local/share/dotnet/sdk]";

        // Act
        var result = _service.ParseDotnetInfo(output);

        // Assert
        Assert.Single(result);
        Assert.Equal(DotnetInfoParserConstants.FrameworkTypeSdk, result[0].Type);
    }

    [Fact]
    public void ParseDotnetInfo_DetectsRuntimeSection()
    {
        // Arrange
        var output = $@"{DotnetInfoParserConstants.RuntimeSectionHeader}
  Microsoft.NETCore.App 9.0.5 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]";

        // Act
        var result = _service.ParseDotnetInfo(output);

        // Assert
        Assert.Single(result);
        Assert.Equal(DotnetInfoParserConstants.FrameworkTypeRuntime, result[0].Type);
    }

    [Fact]
    public void ParseDotnetInfo_DetectsMainSdkSection()
    {
        // Arrange
        var output = $@"{DotnetInfoParserConstants.MainSdkSectionHeader}
 Version:           9.0.301";

        // Act
        var result = _service.ParseDotnetInfo(output);

        // Assert
        Assert.Single(result);
        Assert.Equal(DotnetInfoParserConstants.FrameworkTypeMainSdk, result[0].Type);
    }

    [Fact]
    public void ParseDotnetInfo_IgnoresUnknownLines()
    {
        // Arrange
        var output = "Random content without any section headers";

        // Act
        var result = _service.ParseDotnetInfo(output);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region ParseDotnetInfo - Full Parsing

    [Fact]
    public void ParseDotnetInfo_ValidOutput_ExtractsAllFrameworks()
    {
        // Arrange
        var output = @".NET SDK:
 Version:           9.0.301

.NET SDKs installed:
  9.0.300 [/usr/local/share/dotnet/sdk]
  8.0.404 [/usr/local/share/dotnet/sdk]

.NET runtimes installed:
  Microsoft.AspNetCore.App 9.0.5 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 8.0.11 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 9.0.5 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 8.0.11 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]";

        // Act
        var result = _service.ParseDotnetInfo(output);

        // Assert
        Assert.Single(result, f => f.Type == DotnetInfoParserConstants.FrameworkTypeMainSdk && f.Version == "9.0.301");
        Assert.Single(result, f => f.Type == DotnetInfoParserConstants.FrameworkTypeSdk && f.Version == "9.0.300");
        Assert.Single(result, f => f.Type == DotnetInfoParserConstants.FrameworkTypeSdk && f.Version == "8.0.404");
        Assert.Single(result, f => f.Type == DotnetInfoParserConstants.FrameworkTypeAspNetCore && f.Version == "9.0.5");
        Assert.Single(result, f => f.Type == DotnetInfoParserConstants.FrameworkTypeAspNetCore && f.Version == "8.0.11");
        Assert.Single(result, f => f.Type == DotnetInfoParserConstants.FrameworkTypeRuntime && f.Version == "9.0.5");
        Assert.Single(result, f => f.Type == DotnetInfoParserConstants.FrameworkTypeRuntime && f.Version == "8.0.11");
    }

    [Fact]
    public void ParseDotnetInfo_EmptyOutput_ReturnsEmptyList()
    {
        // Act
        var result = _service.ParseDotnetInfo("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseDotnetInfo_NoVersions_ReturnsEmptyList()
    {
        // Arrange
        var output = "Random content without any version info";

        // Act
        var result = _service.ParseDotnetInfo(output);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region ParseDotnetInfo - Regex Pattern Matching

    [Fact]
    public void ParseDotnetInfo_MatchesSdkVersionPattern()
    {
        // Arrange
        var output = $@"{DotnetInfoParserConstants.SdkSectionHeader}
  9.0.300 [/usr/local/share/dotnet/sdk]";

        // Act
        var result = _service.ParseDotnetInfo(output);

        // Assert
        Assert.Single(result);
        Assert.Equal(DotnetInfoParserConstants.FrameworkTypeSdk, result[0].Type);
        Assert.Equal("9.0.300", result[0].Version);
    }

    [Fact]
    public void ParseDotnetInfo_IgnoresInvalidSdkVersionPattern()
    {
        // Arrange
        var output = $@"{DotnetInfoParserConstants.SdkSectionHeader}
  invalid version line";

        // Act
        var result = _service.ParseDotnetInfo(output);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region ParseDotnetInfo - Ordering

    [Fact]
    public void ParseDotnetInfo_ResultsOrderedCorrectly()
    {
        // Arrange
        var output = @".NET runtimes installed:
  Microsoft.AspNetCore.App 9.0.5 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 9.0.5 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]

.NET SDKs installed:
  9.0.300 [/usr/local/share/dotnet/sdk]

.NET SDK:
 Version:           9.0.301";

        // Act
        var result = _service.ParseDotnetInfo(output);

        // Assert - SDK (Main) first, then SDK, then Runtime, then ASP.NET Core
        Assert.Equal(DotnetInfoParserConstants.FrameworkTypeMainSdk, result[0].Type);
        Assert.Equal(DotnetInfoParserConstants.FrameworkTypeSdk, result[1].Type);
        Assert.Equal(DotnetInfoParserConstants.FrameworkTypeRuntime, result[2].Type);
        Assert.Equal(DotnetInfoParserConstants.FrameworkTypeAspNetCore, result[3].Type);
    }

    #endregion

    #region ParseDotnetInfo - Deduplication

    [Fact]
    public void ParseDotnetInfo_DuplicateVersions_KeepsUnique()
    {
        // Arrange
        var output = @".NET SDKs installed:
  9.0.300 [/usr/local/share/dotnet/sdk]
  9.0.300 [/usr/local/share/dotnet/sdk]";

        // Act
        var result = _service.ParseDotnetInfo(output);

        // Assert
        Assert.Single(result);
        Assert.Equal("9.0.300", result[0].Version);
    }

    #endregion
}
