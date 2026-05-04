using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationRename
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; init; } = default!;

    [JsonPropertyName("additional")]
    public string? Additional { get; init; }
}
