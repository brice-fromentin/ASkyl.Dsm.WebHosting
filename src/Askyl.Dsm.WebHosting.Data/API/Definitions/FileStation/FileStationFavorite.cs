using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

[GenerateClone]
public partial class FileStationFavorite
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("offset")]
    public int? Offset { get; set; } = 0;

    [JsonPropertyName("limit")]
    public int? Limit { get; set; } = 100;

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; set; } = "name";

    [JsonPropertyName("sort_direction")]
    public string? SortDirection { get; set; } = "asc";
}
