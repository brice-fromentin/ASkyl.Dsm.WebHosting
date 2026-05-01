using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationListShare
{
    [JsonPropertyName("offset")]
    public int? Offset { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; init; } // "name", "user", "group", "mtime", "atime", "ctime", "crtime", "posix"

    [JsonPropertyName("sort_direction")]
    public string? SortDirection { get; init; } = "asc"; // "asc" or "desc"

    [JsonPropertyName("onlywritable")]
    public bool? OnlyWritable { get; init; } // true to list only writable shared folders

    [JsonPropertyName("additional")]
    public string? Additional { get; init; } // "real_path,owner,time,perm,mount_point_type,volume_status"
}
