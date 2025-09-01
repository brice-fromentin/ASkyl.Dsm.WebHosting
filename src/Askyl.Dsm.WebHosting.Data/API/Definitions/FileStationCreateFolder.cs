using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationCreateFolder : IGenericCloneable<FileStationCreateFolder>
{
    [JsonPropertyName("folder_path")]
    public string FolderPath { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("force_parent")]
    public bool? ForceParent { get; set; } = false;

    [JsonPropertyName("additional")]
    public string? Additional { get; set; }

    public FileStationCreateFolder Clone()
        => new()
        {
            FolderPath = this.FolderPath,
            Name = this.Name,
            ForceParent = this.ForceParent,
            Additional = this.Additional
        };
}
