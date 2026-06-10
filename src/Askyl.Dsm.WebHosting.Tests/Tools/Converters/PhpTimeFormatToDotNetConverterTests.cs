using Askyl.Dsm.WebHosting.Tools.Converters;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Converters;

public class PhpTimeFormatToDotNetConverterTests
{
    #region Common Time Patterns

    [Theory]
    [InlineData("H:i", "H:mm")]
    [InlineData("H:i:s", "H:mm:ss")]
    [InlineData("h:i a", "h:mm tt")]
    [InlineData("h:i:s a", "h:mm:ss tt")]
    [InlineData("g:i A", "h:mm tt")]
    [InlineData("g:i:s A", "h:mm:ss tt")]
    public void Convert_CommonPatterns_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpTimeFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region 24-Hour Tokens

    [Theory]
    [InlineData("H", "H")]
    [InlineData("G", "H")]
    public void Convert_24HourTokens_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpTimeFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region 12-Hour Tokens

    [Theory]
    [InlineData("h", "h")]
    [InlineData("g", "h")]
    public void Convert_12HourTokens_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpTimeFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Minutes and Seconds

    [Theory]
    [InlineData("i", "mm")]
    [InlineData("s", "ss")]
    public void Convert_MinutesSeconds_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpTimeFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region AM/PM Tokens

    [Theory]
    [InlineData("a", "tt")]
    [InlineData("A", "tt")]
    public void Convert_AmPmTokens_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpTimeFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Ordinal Suffix

    [Fact]
    public void Convert_OrdinalSuffix_ReturnsPlaceholder()
    {
        // Arrange
        const string phpFormat = "S";

        // Act
        var result = PhpTimeFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal("\\th\\st\\nd\\rd\\th", result);
    }

    #endregion

    #region Null / Empty / Whitespace

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Convert_NullOrWhitespace_ReturnsNull(string? phpFormat)
    {
        // Act
        var result = PhpTimeFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Unknown Characters Preserved

    [Fact]
    public void Convert_UnknownCharacters_PreservesSeparators()
    {
        // Arrange
        const string phpFormat = "H:i:s";

        // Act
        var result = PhpTimeFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal("H:mm:ss", result);
    }

    #endregion
}
