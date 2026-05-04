using System.Text.Json.Serialization;


namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;


public record FileStationDownload
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; init; } = [];

    [JsonPropertyName("mode")]
    public string? Mode { get; init; } = "open"; // "open" or "download"
}
