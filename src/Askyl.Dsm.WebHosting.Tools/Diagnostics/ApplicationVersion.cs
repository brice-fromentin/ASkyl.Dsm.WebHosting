using System.Reflection;
using Askyl.Dsm.WebHosting.Constants.Application;

namespace Askyl.Dsm.WebHosting.Tools.Diagnostics;

/// <summary>
/// The version this build carries, for the UI and the logs.
/// </summary>
public static class ApplicationVersion
{
    /// <summary>
    /// Full informational version, such as <c>0.7.0+85dedaa…</c>: the number update-version.sh writes, and the commit
    /// the SDK appends to it. The number alone names every build made between two bumps; the commit names one.
    /// </summary>
    public static string Current { get; } = typeof(ApplicationVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? String.Empty;

    /// <summary>
    /// The version number alone, such as <c>0.7.0</c>.
    /// </summary>
    public static string Number { get; }

    /// <summary>
    /// The full hash of the commit the build came from, or empty for a build made outside a git checkout.
    /// </summary>
    public static string Commit { get; }

    /// <summary>
    /// The commit abbreviated as git abbreviates it, for display; empty when there is no commit.
    /// </summary>
    public static string ShortCommit => Commit[..Math.Min(Commit.Length, ApplicationConstants.ShortCommitLength)];

    /// <summary>
    /// The commit's page on GitHub. Meaningless when <see cref="Commit"/> is empty, and a 404 until the commit is pushed.
    /// </summary>
    public static string CommitUrl => String.Format(ApplicationConstants.RepositoryCommitUrlFormat, Commit);

    static ApplicationVersion() => (Number, Commit) = Split(Current);

    /// <summary>
    /// Splits an informational version into its number and its commit.
    /// </summary>
    /// <param name="informationalVersion">Such as <c>0.7.0+85dedaa…</c>, or <c>0.7.0</c> when no commit was appended.</param>
    /// <returns>The number, and the commit or an empty string.</returns>
    public static (string Number, string Commit) Split(string informationalVersion)
    {
        var separator = informationalVersion.IndexOf(ApplicationConstants.BuildMetadataSeparator);

        return separator < 0 ? (informationalVersion, String.Empty) : (informationalVersion[..separator], informationalVersion[(separator + 1)..]);
    }
}
