using System.Text.Json.Serialization;


namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;


public record FileStationBackgroundTask
{
    [JsonPropertyName("taskid")]
    public string? TaskId { get; init; }

    [JsonPropertyName("offset")]
    public int? Offset { get; init; } = 0;

    [JsonPropertyName("limit")]
    public int? Limit { get; init; } = 100;
}
