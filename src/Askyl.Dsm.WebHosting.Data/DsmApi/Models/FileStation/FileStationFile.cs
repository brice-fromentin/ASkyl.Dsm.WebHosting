using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationFile
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = default!;

    [JsonPropertyName("path")]
    public string Path { get; init; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; init; } = default!; // "file" or "dir"

    [JsonPropertyName("isdir")]
    public bool IsDirectory { get; init; }

    [JsonPropertyName("children")]
    public List<FileStationFile>? Children { get; init; }

    [JsonPropertyName("additional")]
    public FileStationFileAdditional? Additional { get; init; }
}
