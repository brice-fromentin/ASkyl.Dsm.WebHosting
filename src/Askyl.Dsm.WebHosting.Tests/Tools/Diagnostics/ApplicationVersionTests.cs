using System.Text.RegularExpressions;
using Askyl.Dsm.WebHosting.Tools.Diagnostics;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Diagnostics;

public partial class ApplicationVersionTests
{
    [GeneratedRegex(@"^\d+\.\d+\.\d+\+[0-9a-f]{40}$")]
    private static partial Regex NumberPlusCommit();

    [Fact]
    public void Current_NamesTheVersionNumberAndTheCommit()
    {
        // Arrange — the number is what update-version.sh writes; the commit is what tells two builds of one number apart.
        var number = typeof(ApplicationVersion).Assembly.GetName().Version!;

        // Act
        var current = ApplicationVersion.Current;

        // Assert
        Assert.Matches(NumberPlusCommit(), current);
        Assert.StartsWith($"{number.Major}.{number.Minor}.{number.Build}+", current, StringComparison.Ordinal);
    }
}
