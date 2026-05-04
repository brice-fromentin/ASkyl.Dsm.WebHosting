using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants.DSM.FileStation;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationVirtualFolder
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = FileStationDefaults.VirtualFolderTypeAll;

    [JsonPropertyName("offset")]
    public int? Offset { get; init; } = PaginationDefaults.DefaultOffset;

    [JsonPropertyName("limit")]
    public int? Limit { get; init; } = PaginationDefaults.DefaultLimit;

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; init; } = FileStationDefaults.SortByName;

    [JsonPropertyName("sort_direction")]
    public string? SortDirection { get; init; } = FileStationDefaults.SortDirectionAsc;

    [JsonPropertyName("additional")]
    public string? Additional { get; init; }
}
