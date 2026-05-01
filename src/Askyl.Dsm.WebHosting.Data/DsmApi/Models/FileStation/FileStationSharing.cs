using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationSharing
{
    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("password")]
    public string? Password { get; init; }

    [JsonPropertyName("date_expired")]
    public long? DateExpired { get; init; }

    [JsonPropertyName("date_available")]
    public long? DateAvailable { get; init; }

    [JsonPropertyName("offset")]
    public int? Offset { get; init; } = 0;

    [JsonPropertyName("limit")]
    public int? Limit { get; init; } = 100;

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; init; } = "name";

    [JsonPropertyName("sort_direction")]
    public string? SortDirection { get; init; } = "asc";

    [JsonPropertyName("force_secure")]
    public bool? ForceSecure { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }
}
