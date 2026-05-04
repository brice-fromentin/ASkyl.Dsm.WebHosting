using System.Text.Json.Serialization;


namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;


public record FileStationExtract
{
    [JsonPropertyName("file_path")]
    public string FilePath { get; init; } = default!;

    [JsonPropertyName("dest_folder_path")]
    public string DestFolderPath { get; init; } = default!;

    [JsonPropertyName("overwrite")]
    public bool? Overwrite { get; init; } = false;

    [JsonPropertyName("keep_dir")]
    public bool? KeepDir { get; init; } = true;

    [JsonPropertyName("create_subfolder")]
    public bool? CreateSubfolder { get; init; } = false;

    [JsonPropertyName("codepage")]
    public string? Codepage { get; init; } = "utf-8";

    [JsonPropertyName("password")]
    public string? Password { get; init; }

    [JsonPropertyName("item_id")]
    public List<int>? ItemIds { get; init; }
}
