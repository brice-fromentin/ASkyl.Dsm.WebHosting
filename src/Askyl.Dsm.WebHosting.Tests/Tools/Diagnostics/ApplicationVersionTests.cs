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

    [Fact]
    public void NumberAndCommit_AreCurrentSplitAtThePlus()
    {
        // Assert
        Assert.Equal($"{ApplicationVersion.Number}+{ApplicationVersion.Commit}", ApplicationVersion.Current);
        Assert.Equal(7, ApplicationVersion.ShortCommit.Length);
        Assert.StartsWith(ApplicationVersion.ShortCommit, ApplicationVersion.Commit, StringComparison.Ordinal);
    }

    [Fact]
    public void CommitUrl_PointsAtTheCommitOnGitHub()
    {
        // Assert — written out in full, so a wrong owner, repository or path segment cannot hide behind a shared constant.
        Assert.Equal($"https://github.com/brice-fromentin/ASkyl.Dsm.WebHosting/commit/{ApplicationVersion.Commit}", ApplicationVersion.CommitUrl);
    }

    [Theory]
    [InlineData("0.7.0+50e779b", "0.7.0", "50e779b")]
    [InlineData("0.7.0", "0.7.0", "")]
    public void Split_SeparatesTheNumberFromTheCommit_AndToleratesABuildWithoutOne(string informationalVersion, string number, string commit)
    {
        // Arrange — a build outside a git checkout, from a source archive for instance, carries no commit: the header
        // must lose its link, not the application its start.

        // Act
        var result = ApplicationVersion.Split(informationalVersion);

        // Assert
        Assert.Equal((number, commit), result);
    }
}
