using System.Text.Json;

namespace Askyl.Dsm.WebHosting.Constants.JSON;

/// <summary>
/// Cached JsonSerializerOptions for consistent JSON serialization/deserialization behavior.
/// </summary>
public static class JsonOptionsCache
{
    /// <summary>
    /// Shared JsonSerializerOptions instance with standard configuration.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        MaxDepth = 64
    };

    /// <summary>
    /// JsonSerializerOptions with indented formatting for human-readable output.
    /// </summary>
    public static readonly JsonSerializerOptions WriteIndented = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        MaxDepth = 64,
        WriteIndented = true
    };
}
