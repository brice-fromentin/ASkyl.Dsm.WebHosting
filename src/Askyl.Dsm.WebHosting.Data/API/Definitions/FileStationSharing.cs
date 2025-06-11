using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationSharing : IGenericCloneable<FileStationSharing>
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("date_expired")]
    public long? DateExpired { get; set; }

    [JsonPropertyName("date_available")]
    public long? DateAvailable { get; set; }

    [JsonPropertyName("offset")]
    public int? Offset { get; set; } = 0;

    [JsonPropertyName("limit")]
    public int? Limit { get; set; } = 100;

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; set; } = "name";

    [JsonPropertyName("sort_direction")]
    public string? SortDirection { get; set; } = "asc";

    [JsonPropertyName("force_secure")]
    public bool? ForceSecure { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    public FileStationSharing Clone()
        => new()
        {
            Path = this.Path,
            Password = this.Password,
            DateExpired = this.DateExpired,
            DateAvailable = this.DateAvailable,
            Offset = this.Offset,
            Limit = this.Limit,
            SortBy = this.SortBy,
            SortDirection = this.SortDirection,
            ForceSecure = this.ForceSecure,
            Id = this.Id
        };
}
