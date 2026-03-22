using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

[GenerateClone]
public partial class FileStationListShare
{
    [JsonPropertyName("offset")]
    public int? Offset { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; set; } // "name", "user", "group", "mtime", "atime", "ctime", "crtime", "posix"

    [JsonPropertyName("sort_direction")]
    public string? SortDirection { get; set; } = "asc"; // "asc" or "desc"

    [JsonPropertyName("onlywritable")]
    public bool? OnlyWritable { get; set; } // true to list only writable shared folders

    [JsonPropertyName("additional")]
    public string? Additional { get; set; } // "real_path,owner,time,perm,mount_point_type,volume_status"
}
