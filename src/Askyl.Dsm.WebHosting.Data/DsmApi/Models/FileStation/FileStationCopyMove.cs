using System.Text.Json.Serialization;


namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;


public record FileStationCopyMove
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; init; } = [];

    [JsonPropertyName("dest_folder_path")]
    public string DestFolderPath { get; init; } = default!;

    [JsonPropertyName("overwrite")]
    public bool? Overwrite { get; init; } = false;

    [JsonPropertyName("remove_src")]
    public bool? RemoveSource { get; init; } = false; // true for move, false for copy

    [JsonPropertyName("accurate_progress")]
    public bool? AccurateProgress { get; init; } = false;

    [JsonPropertyName("search_taskid")]
    public string? SearchTaskId { get; init; }
}
