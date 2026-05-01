using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationFavorite
{
    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("offset")]
    public int? Offset { get; init; } = 0;

    [JsonPropertyName("limit")]
    public int? Limit { get; init; } = 100;

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; init; } = "name";

    [JsonPropertyName("sort_direction")]
    public string? SortDirection { get; init; } = "asc";
}
