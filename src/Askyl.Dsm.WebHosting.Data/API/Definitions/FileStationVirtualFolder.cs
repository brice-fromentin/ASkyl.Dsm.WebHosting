using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationVirtualFolder : IGenericCloneable<FileStationVirtualFolder>
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

    public FileStationVirtualFolder Clone()
        => new()
        {
            Type = this.Type,
            Offset = this.Offset,
            Limit = this.Limit,
            SortBy = this.SortBy,
            SortDirection = this.SortDirection,
            Additional = this.Additional
        };
}
