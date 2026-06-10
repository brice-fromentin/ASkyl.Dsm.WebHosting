using Askyl.Dsm.WebHosting.Tools.Converters;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Converters;

public class DsmLanguageToCultureConverterTests
{
    #region Valid DSM Codes

    [Theory]
    [InlineData("enu", "en-US")]
    [InlineData("fra", "fr-FR")]
    [InlineData("deu", "de-DE")]
    [InlineData("ita", "it-IT")]
    [InlineData("spa", "es-ES")]
    [InlineData("jpn", "ja-JP")]
    [InlineData("kor", "ko-KR")]
    [InlineData("rus", "ru-RU")]
    [InlineData("nld", "nl-NL")]
    [InlineData("pol", "pl-PL")]
    [InlineData("ces", "cs-CZ")]
    [InlineData("hun", "hu-HU")]
    [InlineData("sve", "sv-SE")]
    [InlineData("trk", "tr-TR")]
    [InlineData("ukr", "uk-UA")]
    [InlineData("heb", "he-IL")]
    [InlineData("tha", "th-TH")]
    [InlineData("nor", "nb-NO")]
    [InlineData("dan", "da-DK")]
    [InlineData("ptb", "pt-PT")]
    [InlineData("ptg", "pt-BR")]
    public void Convert_ValidCodes_ReturnsCulture(string code, string expected)
    {
        // Act
        var result = DsmLanguageToCultureConverter.Convert(code);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Supplang Variants

    [Theory]
    [InlineData("fre", "fr-FR")]
    [InlineData("ger", "de-DE")]
    [InlineData("spn", "es-ES")]
    [InlineData("krn", "ko-KR")]
    [InlineData("plk", "pl-PL")]
    [InlineData("swe", "sv-SE")]
    [InlineData("tur", "tr-TR")]
    [InlineData("csy", "cs-CZ")]
    public void Convert_SupplangVariants_ReturnsCulture(string code, string expected)
    {
        // Act
        var result = DsmLanguageToCultureConverter.Convert(code);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Chinese Variants

    [Theory]
    [InlineData("chs", "zh-CN")]
    [InlineData("cht", "zh-TW")]
    [InlineData("zhs", "zh-CN")]
    [InlineData("zht", "zh-TW")]
    [InlineData("zhcn", "zh-CN")]
    [InlineData("zhtw", "zh-TW")]
    public void Convert_ChineseVariants_ReturnsCulture(string code, string expected)
    {
        // Act
        var result = DsmLanguageToCultureConverter.Convert(code);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Browser Default

    [Theory]
    [InlineData("def")]
    [InlineData("DEF")]
    [InlineData("Def")]
    public void Convert_BrowserDefault_ReturnsNull(string code)
    {
        // Act
        var result = DsmLanguageToCultureConverter.Convert(code);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Null / Empty / Whitespace

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Convert_NullOrWhitespace_ReturnsNull(string? code)
    {
        // Act
        var result = DsmLanguageToCultureConverter.Convert(code);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Unknown Codes

    [Theory]
    [InlineData("xyz")]
    [InlineData("abc")]
    [InlineData("invalid")]
    public void Convert_UnknownCode_ReturnsNull(string code)
    {
        // Act
        var result = DsmLanguageToCultureConverter.Convert(code);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Case Insensitivity

    [Theory]
    [InlineData("ENU", "en-US")]
    [InlineData("Fra", "fr-FR")]
    [InlineData("dEu", "de-DE")]
    public void Convert_CaseInsensitive_Matches(string code, string expected)
    {
        // Act
        var result = DsmLanguageToCultureConverter.Convert(code);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Constant

    [Fact]
    public void DefaultBrowser_HasExpectedValue()
    {
        // Assert
        Assert.Equal("def", DsmLanguageToCultureConverter.DefaultBrowser);
    }

    #endregion
}
