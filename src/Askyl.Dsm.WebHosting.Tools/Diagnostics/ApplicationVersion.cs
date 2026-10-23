using System.Reflection;

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
}
