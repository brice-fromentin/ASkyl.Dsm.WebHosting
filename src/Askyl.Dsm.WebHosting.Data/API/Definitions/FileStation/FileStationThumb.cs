using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

[GenerateClone]
public partial class FileStationThumb
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("size")]
    public string? Size { get; set; } = "medium"; // "small", "medium", "large", "original"

    [JsonPropertyName("rotate")]
    public int? Rotate { get; set; } = 0; // 0, 90, 180, 270
}
