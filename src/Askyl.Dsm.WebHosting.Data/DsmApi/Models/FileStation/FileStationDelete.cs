using System.Text.Json.Serialization;


namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;


public record FileStationDelete
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; init; } = [];

    [JsonPropertyName("accurate_progress")]
    public bool? AccurateProgress { get; init; } = false;

    [JsonPropertyName("recursive")]
    public bool? Recursive { get; init; } = true;
}
