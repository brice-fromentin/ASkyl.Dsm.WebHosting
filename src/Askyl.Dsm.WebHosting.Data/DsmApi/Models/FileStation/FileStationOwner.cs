using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationOwner
{
    [JsonPropertyName("user")]
    public string? User { get; init; }

    [JsonPropertyName("group")]
    public string? Group { get; init; }

    [JsonPropertyName("uid")]
    public int? Uid { get; init; }

    [JsonPropertyName("gid")]
    public int? Gid { get; init; }
}
