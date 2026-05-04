using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationSearch
{
    [JsonPropertyName("folder_path")]
    public string FolderPath { get; init; } = "/";

    [JsonPropertyName("recursive")]
    public bool? Recursive { get; init; } = true;

    [JsonPropertyName("pattern")]
    public string? Pattern { get; init; }

    [JsonPropertyName("extension")]
    public string? Extension { get; init; }

    [JsonPropertyName("filetype")]
    public string? FileType { get; init; } // "file", "dir", "all"

    [JsonPropertyName("size_from")]
    public long? SizeFrom { get; init; }

    [JsonPropertyName("size_to")]
    public long? SizeTo { get; init; }

    [JsonPropertyName("mtime_from")]
    public long? MTimeFrom { get; init; }

    [JsonPropertyName("mtime_to")]
    public long? MTimeTo { get; init; }

    [JsonPropertyName("crtime_from")]
    public long? CrTimeFrom { get; init; }

    [JsonPropertyName("crtime_to")]
    public long? CrTimeTo { get; init; }

    [JsonPropertyName("atime_from")]
    public long? ATimeFrom { get; init; }

    [JsonPropertyName("atime_to")]
    public long? ATimeTo { get; init; }

    [JsonPropertyName("owner")]
    public string? Owner { get; init; }

    [JsonPropertyName("group")]
    public string? Group { get; init; }
}
