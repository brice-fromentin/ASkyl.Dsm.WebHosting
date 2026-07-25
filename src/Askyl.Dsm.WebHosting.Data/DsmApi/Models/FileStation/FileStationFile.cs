using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants.DSM.FileStation;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

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
