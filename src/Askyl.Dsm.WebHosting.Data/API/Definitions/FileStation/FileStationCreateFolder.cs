using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

[GenerateClone]
public partial class FileStationCreateFolder
{
    [JsonPropertyName("folder_path")]
    public string FolderPath { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("force_parent")]
    public bool? ForceParent { get; set; } = false;

    [JsonPropertyName("additional")]
    public string? Additional { get; set; }
}
