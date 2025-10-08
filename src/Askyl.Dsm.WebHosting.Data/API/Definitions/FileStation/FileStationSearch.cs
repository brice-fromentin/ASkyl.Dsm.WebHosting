using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class FileStationSearch
{
    [JsonPropertyName("folder_path")]
    public string FolderPath { get; set; } = "/";

    [JsonPropertyName("recursive")]
    public bool? Recursive { get; set; } = true;

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    [JsonPropertyName("extension")]
    public string? Extension { get; set; }

    [JsonPropertyName("filetype")]
    public string? FileType { get; set; } // "file", "dir", "all"

    [JsonPropertyName("size_from")]
    public long? SizeFrom { get; set; }

    [JsonPropertyName("size_to")]
    public long? SizeTo { get; set; }

    [JsonPropertyName("mtime_from")]
    public long? MTimeFrom { get; set; }

    [JsonPropertyName("mtime_to")]
    public long? MTimeTo { get; set; }

    [JsonPropertyName("crtime_from")]
    public long? CrTimeFrom { get; set; }

    [JsonPropertyName("crtime_to")]
    public long? CrTimeTo { get; set; }

    [JsonPropertyName("atime_from")]
    public long? ATimeFrom { get; set; }

    [JsonPropertyName("atime_to")]
    public long? ATimeTo { get; set; }

    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("group")]
    public string? Group { get; set; }
}
