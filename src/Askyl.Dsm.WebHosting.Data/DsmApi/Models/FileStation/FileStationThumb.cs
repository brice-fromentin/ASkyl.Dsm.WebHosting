using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationThumb
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = default!;

    [JsonPropertyName("size")]
    public string? Size { get; init; } = "medium"; // "small", "medium", "large", "original"

    [JsonPropertyName("rotate")]
    public int? Rotate { get; init; } = 0; // 0, 90, 180, 270
}
