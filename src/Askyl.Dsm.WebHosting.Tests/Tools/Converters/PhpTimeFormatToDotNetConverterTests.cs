using Askyl.Dsm.WebHosting.Tools.Converters;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Converters;

public class PhpTimeFormatToDotNetConverterTests
{
    #region Common Time Patterns

    [Theory]
    [InlineData("H:i", "HH:mm")]
    [InlineData("H:i:s", "HH:mm:ss")]
    [InlineData("h:i a", "hh:mm tt")]
    [InlineData("h:i:s a", "hh:mm:ss tt")]
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
    [InlineData("H", "HH")]  // PHP H: 00-23 with leading zero → .NET HH
    [InlineData("G", "H")]  // PHP G: 0-23 without leading zero → .NET H
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
    [InlineData("h", "hh")]  // PHP h: 01-12 with leading zero → .NET hh
    [InlineData("g", "h")]  // PHP g: 1-12 without leading zero → .NET h
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

    /// <summary>
    /// PHP 'S' (ordinal suffix: st, nd, rd, th) has no .NET equivalent.
    /// The token is passed through unchanged.
    /// </summary>
    [Fact]
    public void Convert_OrdinalSuffix_PassThrough()
    {
        // Arrange
        const string phpFormat = "S";

        // Act
        var result = PhpTimeFormatToDotNetConverter.Convert(phpFormat);

        // Assert — 'S' is not mapped, so it passes through as-is
        Assert.Equal("S", result);
    }

    #endregion

    #region PHP Escape Mechanism

    [Theory]
    [InlineData("\\i", "i")]    // \i → literal i (not minutes mm)
    [InlineData("\\H", "H")]    // \H → literal H (not hour HH)
    [InlineData("H\\:i", "HH:mm")]  // H\:i → HH-literal colon-mm
    public void Convert_EscapedCharacters_ReturnsLiterals(string phpFormat, string expected)
    {
        // Act
        var result = PhpTimeFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
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

    #region Separator Preservation

    [Fact]
    public void Convert_KnownTokens_PreservesSeparators()
    {
        // Arrange
        const string phpFormat = "H:i:s";

        // Act
        var result = PhpTimeFormatToDotNetConverter.Convert(phpFormat);

        // Assert — colon separators are preserved between converted tokens
        Assert.Equal("HH:mm:ss", result);
    }

    #endregion
}
