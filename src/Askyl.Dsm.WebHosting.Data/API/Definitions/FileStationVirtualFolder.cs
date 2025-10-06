using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class FileStationVirtualFolder
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = FileStationDefaults.VirtualFolderTypeAll;

    [JsonPropertyName("offset")]
    public int? Offset { get; set; } = DsmPaginationDefaults.DefaultOffset;

    [JsonPropertyName("limit")]
    public int? Limit { get; set; } = DsmPaginationDefaults.DefaultLimit;

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; set; } = FileStationDefaults.SortByName;

    [JsonPropertyName("sort_direction")]
    public string? SortDirection { get; set; } = FileStationDefaults.SortDirectionAsc;

    [JsonPropertyName("additional")]
    public string? Additional { get; set; }
}
