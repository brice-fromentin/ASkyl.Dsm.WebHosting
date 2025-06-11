using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationList : IGenericCloneable<FileStationList>
{
    [JsonPropertyName("folder_path")]
    public string FolderPath { get; set; } = "/";

    [JsonPropertyName("offset")]
    public int? Offset { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; set; } // "name", "size", "user", "group", "mtime", "atime", "ctime", "crtime", "posix", "type"

    [JsonPropertyName("sort_direction")]
    public string? SortDirection { get; set; } = "asc"; // "asc" or "desc"

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; } // search pattern

    [JsonPropertyName("filetype")]
    public string? FileType { get; set; } // "file", "dir", "all"

    [JsonPropertyName("goto_path")]
    public string? GotoPath { get; set; } // jump to a specific path

    [JsonPropertyName("additional")]
    public string? Additional { get; set; } // "real_path,size,owner,time,perm,mount_point_type,type"

    public FileStationList Clone()
        => new()
        {
            FolderPath = this.FolderPath,
            Offset = this.Offset,
            Limit = this.Limit,
            SortBy = this.SortBy,
            SortDirection = this.SortDirection,
            Pattern = this.Pattern,
            FileType = this.FileType,
            GotoPath = this.GotoPath,
            Additional = this.Additional
        };
}
