using System.Globalization;
using Askyl.Dsm.WebHosting.Constants.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Extensions;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

public class CultureInfoExtensionsTests
{
    [Fact]
    public void GetTextDirection_LtrCulture_ReturnsLtr()
    {
        var culture = new CultureInfo("en-US");

        var direction = culture.GetTextDirection();

        Assert.Equal(GlobalizationConstants.TextDirectionLtr, direction);
    }

    [Fact]
    public void GetTextDirection_RtlCulture_ReturnsRtl()
    {
        var culture = new CultureInfo("he-IL");

        var direction = culture.GetTextDirection();

        Assert.Equal(GlobalizationConstants.TextDirectionRtl, direction);
    }
}
