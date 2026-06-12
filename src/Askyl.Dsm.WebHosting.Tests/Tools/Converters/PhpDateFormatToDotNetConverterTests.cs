using Askyl.Dsm.WebHosting.Tools.Converters;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Converters;

public class PhpDateFormatToDotNetConverterTests
{
    #region Common Format Patterns

    [Theory]
    [InlineData("Y/m/d", "yyyy/MM/dd")]
    [InlineData("d/m/Y", "dd/MM/yyyy")]
    [InlineData("m/d/Y", "MM/dd/yyyy")]
    [InlineData("Y-m-d", "yyyy-MM-dd")]
    [InlineData("j/n/Y", "d/M/yyyy")]
    public void Convert_CommonPatterns_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpDateFormatToDotNetConverter.Convert(phpFormat);

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
        var result = PhpDateFormatToDotNetConverter.Convert(phpFormat);

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
        var result = PhpDateFormatToDotNetConverter.Convert(phpFormat);

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
        var result = PhpDateFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Day of Week Tokens

    /// <summary>
    /// PHP 'w' (0=Sun..6=Sat) and 'N' (1=Mon..7=Sun) have no direct .NET equivalent.
    /// Both tokens are passed through unchanged as literals.
    /// </summary>
    [Theory]
    [InlineData("w", "w")]
    [InlineData("N", "N")]
    public void Convert_DayOfWeekTokens_PassThrough(string phpFormat, string expected)
    {
        // Act
        var result = PhpDateFormatToDotNetConverter.Convert(phpFormat);

        // Assert — not mapped, so passed through as-is
        Assert.Equal(expected, result);
    }

    #endregion

    #region Day of Year Tokens

    [Theory]
    [InlineData("z", "%j")]
    public void Convert_DayOfYearTokens_ReturnsExpected(string phpFormat, string expected)
    {
        // Act
        var result = PhpDateFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region PHP Escape Mechanism

    /// <summary>
    /// In PHP date formats, \x means literal 'x'. Test that escape handling works.
    /// Note: In C# strings, \\ represents a single backslash.
    /// </summary>
    [Theory]
    [InlineData("\\m", "m")]       // \m → literal m (not month MM)
    [InlineData("\\Y", "Y")]       // \Y → literal Y (not year yyyy)
    [InlineData("\\d", "d")]       // \d → literal d (not day dd)
    [InlineData("Y\\-m", "yyyy-MM")]  // Y\-m → yyyy-literal dash-MM
    public void Convert_EscapedCharacters_ReturnsLiterals(string phpFormat, string expected)
    {
        // Act
        var result = PhpDateFormatToDotNetConverter.Convert(phpFormat);

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
    public void Convert_ComplexPatterns_PreservesSeparators(string phpFormat, string expected)
    {
        // Act
        var result = PhpDateFormatToDotNetConverter.Convert(phpFormat);

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
        var result = PhpDateFormatToDotNetConverter.Convert(phpFormat);

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
        var result = PhpDateFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal("yyyy.MM.dd", result);
    }

    #endregion
}
