using Askyl.Dsm.WebHosting.Tools.Converters;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Converters;

public class PhpFormatToDotNetConverterTests
{
    #region Common Format Patterns

    [Theory]
    [InlineData("Y/m/d", "yyyy/MM/dd")]
    [InlineData("d/m/Y", "dd/MM/yyyy")]
    [InlineData("m/d/Y", "MM/dd/yyyy")]
    [InlineData("Y-m-d", "yyyy-MM-dd")]
    [InlineData("j/n/Y", "d/M/yyyy")]
    [InlineData("H:i", "HH:mm")]
    [InlineData("H:i:s", "HH:mm:ss")]
    [InlineData("h:i a", "hh:mm tt")]
    [InlineData("h:i:s a", "hh:mm:ss tt")]
    public void Convert_CommonPatterns_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Year Tokens

    [Theory]
    [InlineData("Y", "yyyy")]
    [InlineData("y", "yy")]
    public void Convert_YearTokens_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Month Tokens

    [Theory]
    [InlineData("m", "MM")]
    [InlineData("n", "M")]
    [InlineData("M", "MMM")]
    [InlineData("F", "MMMM")]
    public void Convert_MonthTokens_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Day Tokens

    [Theory]
    [InlineData("d", "dd")]
    [InlineData("j", "d")]
    [InlineData("l", "dddd")]
    [InlineData("D", "ddd")]
    public void Convert_DayTokens_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Hour Tokens

    [Theory]
    [InlineData("H", "HH")]
    [InlineData("G", "H")]
    [InlineData("h", "hh")]
    [InlineData("g", "h")]
    public void Convert_HourTokens_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Minutes, Seconds, AM/PM

    [Theory]
    [InlineData("i", "mm")]
    [InlineData("s", "ss")]
    [InlineData("a", "tt")]
    [InlineData("A", "tt")]
    public void Convert_MinutesSecondsAmPm_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Day of Year / Timezone

    [Theory]
    [InlineData("z", "%j")]
    [InlineData("P", "zzz")]
    public void Convert_DayOfYearTimezone_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Unmapped Tokens Pass Through

    [Theory]
    [InlineData("w", "w")]
    [InlineData("N", "N")]
    [InlineData("e", "e")]
    [InlineData("T", "T")]
    [InlineData("O", "O")]
    [InlineData("I", "I")]
    [InlineData("Z", "Z")]
    [InlineData("S", "S")]
    public void Convert_UnmappedTokens_PassThrough(string phpFormat, string expected)
    {
        // Act
        var result = PhpFormatToDotNetConverter.Convert(phpFormat);

        // Assert — no .NET equivalent, passed through as literal
        Assert.Equal(expected, result);
    }

    #endregion

    #region PHP Escape Mechanism

    [Theory]
    [InlineData("\\m", "m")]
    [InlineData("\\Y", "Y")]
    [InlineData("\\d", "d")]
    [InlineData("\\H", "H")]
    [InlineData("\\i", "i")]
    [InlineData("Y\\-m", "yyyy-MM")]
    [InlineData("H\\:i", "HH:mm")]
    public void Convert_EscapedCharacters_ReturnsLiterals(string phpFormat, string expected)
    {
        // Act
        var result = PhpFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Complex Patterns

    [Theory]
    [InlineData("j M Y", "d MMM yyyy")]
    [InlineData("F j, Y", "MMMM d, yyyy")]
    [InlineData("l, F j, Y", "dddd, MMMM d, yyyy")]
    [InlineData("D, M j, Y", "ddd, MMM d, yyyy")]
    [InlineData("Y-m-d P", "yyyy-MM-dd zzz")]
    [InlineData("g:i A", "h:mm tt")]
    [InlineData("g:i:s A", "h:mm:ss tt")]
    public void Convert_ComplexPatterns_PreservesSeparators(string phpFormat, string expected)
    {
        // Act
        var result = PhpFormatToDotNetConverter.Convert(phpFormat);

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
        var result = PhpFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Unknown Characters Preserved

    [Fact]
    public void Convert_UnknownCharacters_PreservesSeparators()
    {
        // Arrange
        const string phpFormat = "Y.m.d";

        // Act
        var result = PhpFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal("yyyy.MM.dd", result);
    }

    #endregion
}
