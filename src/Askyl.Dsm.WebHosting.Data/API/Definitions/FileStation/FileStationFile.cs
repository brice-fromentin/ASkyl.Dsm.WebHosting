using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class FileStationFile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = default!; // "file" or "dir"

    [JsonPropertyName("isdir")]
    public bool IsDirectory { get; set; }

    [JsonPropertyName("children")]
    public List<FileStationFile>? Children { get; set; }

    [JsonPropertyName("additional")]
    public FileStationFileAdditional? Additional { get; set; }
}
