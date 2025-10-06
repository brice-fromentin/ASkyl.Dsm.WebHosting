using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class FileStationExtract
{
    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = default!;

    [JsonPropertyName("dest_folder_path")]
    public string DestFolderPath { get; set; } = default!;

    [JsonPropertyName("overwrite")]
    public bool? Overwrite { get; set; } = false;

    [JsonPropertyName("keep_dir")]
    public bool? KeepDir { get; set; } = true;

    [JsonPropertyName("create_subfolder")]
    public bool? CreateSubfolder { get; set; } = false;

    [JsonPropertyName("codepage")]
    public string? Codepage { get; set; } = "utf-8";

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("item_id")]
    public List<int>? ItemIds { get; set; }
}
