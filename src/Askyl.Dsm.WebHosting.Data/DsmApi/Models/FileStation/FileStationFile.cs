using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

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

public record FileStationFile
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = default!;

    [JsonPropertyName("path")]
    public string Path { get; init; } = default!;

    [JsonPropertyName("type")]
    public FileStationType Type { get; init; }

    [JsonPropertyName("isdir")]
    public bool IsDirectory { get; init; }

    [JsonPropertyName("children")]
    public List<FileStationFile>? Children { get; init; }

    [JsonPropertyName("additional")]
    public FileStationFileAdditional? Additional { get; init; }
}
