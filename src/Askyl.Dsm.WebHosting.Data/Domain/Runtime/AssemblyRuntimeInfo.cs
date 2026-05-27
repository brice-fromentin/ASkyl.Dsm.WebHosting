using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Domain.Runtime;

/// <summary>
/// Represents the detected .NET runtime information for an assembly file.
/// </summary>
/// <param name="Channel">The framework channel (e.g., "8.0").</param>
/// <param name="IsCompatible">Whether the required runtime is installed on the system.</param>
/// <param name="MissingMessage">Human-readable error message if the runtime is not installed.</param>
public record AssemblyRuntimeInfo(
    string Channel,
    bool IsCompatible,
    string? MissingMessage)
{
    /// <summary>
    /// Initializes a new instance with compatibility status.
    /// </summary>
    [JsonConstructor]
    public AssemblyRuntimeInfo() : this("", false, null) { }
}
