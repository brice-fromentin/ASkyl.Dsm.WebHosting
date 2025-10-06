using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class FileStationCheckPermission
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("overwrite")]
    public bool? Overwrite { get; set; }
}
