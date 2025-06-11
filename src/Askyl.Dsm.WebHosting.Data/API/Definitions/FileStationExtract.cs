using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationExtract : IGenericCloneable<FileStationExtract>
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

    public FileStationExtract Clone()
        => new()
        {
            FilePath = this.FilePath,
            DestFolderPath = this.DestFolderPath,
            Overwrite = this.Overwrite,
            KeepDir = this.KeepDir,
            CreateSubfolder = this.CreateSubfolder,
            Codepage = this.Codepage,
            Password = this.Password,
            ItemIds = this.ItemIds?.ToList()
        };
}
