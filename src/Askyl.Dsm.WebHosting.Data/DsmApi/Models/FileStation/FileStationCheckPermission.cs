using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationCheckPermission
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = default!;

    [JsonPropertyName("filename")]
    public string? Filename { get; init; }

    [JsonPropertyName("overwrite")]
    public bool? Overwrite { get; init; }
}
