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

    #region Day of Year Tokens

    [Theory]
    [InlineData("z", "%j")]
    [InlineData("w", "%u")]
    [InlineData("N", "%u")]
    public void Convert_DayOfYearTokens_ReturnsExpected(string phpFormat, string expected)
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
        var phpFormat = "Y.m.d";

        // Act
        var result = PhpDateFormatToDotNetConverter.Convert(phpFormat);

        // Assert
        Assert.Equal("yyyy.MM.dd", result);
    }

    #endregion
}
