namespace Askyl.Dsm.WebHosting.Data.Exceptions;

/// <summary>
/// Exception thrown when attempting to uninstall the last remaining release of a configured channel.
/// This prevents accidental removal of the only available runtime for the application.
/// </summary>
public sealed class LastReleaseUninstallException : InvalidOperationException
{
    /// <summary>
    /// The version that was attempted to be uninstalled.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// The configured channel for which this is the last release.
    /// </summary>
    public string Channel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LastReleaseUninstallException"/> class.
    /// </summary>
    /// <param name="version">The version that was attempted to be uninstalled.</param>
    /// <param name="channel">The configured channel for which this is the last release.</param>
    public LastReleaseUninstallException(string version, string channel)
        : base($"Cannot uninstall ASP.NET Core {version}: it's the only installed release for channel {channel}.")
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LastReleaseUninstallException"/> class with a custom message.
    /// </summary>
    /// <param name="version">The version that was attempted to be uninstalled.</param>
    /// <param name="channel">The configured channel for which this is the last release.</param>
    /// <param name="message">The custom error message.</param>
    public LastReleaseUninstallException(string version, string channel, string message)
        : base(message)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LastReleaseUninstallException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="version">The version that was attempted to be uninstalled.</param>
    /// <param name="channel">The configured channel for which this is the last release.</param>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public LastReleaseUninstallException(string version, string channel, string message, Exception innerException)
        : base(message, innerException)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }
}
