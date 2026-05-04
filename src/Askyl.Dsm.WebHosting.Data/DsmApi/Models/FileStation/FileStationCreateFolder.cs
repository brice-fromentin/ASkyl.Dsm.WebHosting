using System.Text.Json.Serialization;


namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;


public record FileStationCreateFolder
{
    [JsonPropertyName("folder_path")]
    public string FolderPath { get; init; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; init; } = default!;

    [JsonPropertyName("force_parent")]
    public bool? ForceParent { get; init; } = false;

    [JsonPropertyName("additional")]
    public string? Additional { get; init; }
}
