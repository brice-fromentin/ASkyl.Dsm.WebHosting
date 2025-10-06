using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class FileStationCopyMove
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; set; } = [];

    [JsonPropertyName("dest_folder_path")]
    public string DestFolderPath { get; set; } = default!;

    [JsonPropertyName("overwrite")]
    public bool? Overwrite { get; set; } = false;

    [JsonPropertyName("remove_src")]
    public bool? RemoveSource { get; set; } = false; // true for move, false for copy

    [JsonPropertyName("accurate_progress")]
    public bool? AccurateProgress { get; set; } = false;

    [JsonPropertyName("search_taskid")]
    public string? SearchTaskId { get; set; }
}
