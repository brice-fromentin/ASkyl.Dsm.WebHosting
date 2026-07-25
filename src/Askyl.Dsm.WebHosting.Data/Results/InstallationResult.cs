using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Result type for framework/runtime installation operations.
/// </summary>
public sealed class InstallationResult(bool success, string? message, string? version, DateTime? installedAt, ApiErrorCode errorCode = default)
    : ApiResult(success, message, errorCode)
{
    [JsonConstructor]
    private InstallationResult() : this(false, null, null, null, ApiErrorCode.Failure) { }

    /// <summary>
    /// The installed version if the operation was successful. Null if failed.
    /// </summary>
    public string? Version { get; init; } = version;

    /// <summary>
    /// Timestamp when installation was completed. Null if failed.
    /// </summary>
    public DateTime? InstalledAt { get; init; } = installedAt;

    /// <summary>
    /// Creates a successful installation result with the installed version and timestamp.
    /// </summary>
    /// <param name="version">The version that was installed.</param>
    /// <param name="message">Optional success message (defaults to "Installation completed successfully.").</param>
    public static InstallationResult CreateSuccess(string version, string message = "Installation completed successfully.")
        => new(true, message, version, DateTime.UtcNow, ApiErrorCode.None);

    /// <summary>
    /// Creates a failed installation result.
    /// </summary>
    /// <param name="message">Error message describing the installation failure.</param>
    public static new InstallationResult CreateFailure(string message)
        => new(false, message, null, null, ApiErrorCode.Failure);
}
