using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Constants.DSM.FileStation;

/// <summary>
/// Represents the type of a FileStation entry.
/// </summary>
public enum FileStationType
{
    /// <summary>
    /// A regular file.
    /// </summary>
    [JsonPropertyName("file")]
    File,

    /// <summary>
    /// A directory.
    /// </summary>
    [JsonPropertyName("dir")]
    Directory
}
