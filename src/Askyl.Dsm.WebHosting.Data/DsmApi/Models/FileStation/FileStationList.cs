using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationList
{
    [JsonPropertyName("folder_path")]
    public string FolderPath { get; init; } = "/";

    [JsonPropertyName("offset")]
    public int? Offset { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; init; } // "name", "size", "user", "group", "mtime", "atime", "ctime", "crtime", "posix", "type"

    [JsonPropertyName("sort_direction")]
    public string? SortDirection { get; init; } = "asc"; // "asc" or "desc"

    [JsonPropertyName("pattern")]
    public string? Pattern { get; init; } // search pattern

    [JsonPropertyName("filetype")]
    public string? FileType { get; init; } // "file", "dir", "all"

    [JsonPropertyName("goto_path")]
    public string? GotoPath { get; init; } // jump to a specific path

    [JsonPropertyName("additional")]
    public string? Additional { get; init; } // "real_path,size,owner,time,perm,mount_point_type,type"
}
