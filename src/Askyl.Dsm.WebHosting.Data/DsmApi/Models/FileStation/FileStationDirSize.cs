using System.Text.Json.Serialization;


namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;


public record FileStationDirSize
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; init; } = [];
}
