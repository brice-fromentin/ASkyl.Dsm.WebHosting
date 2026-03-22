using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

[GenerateClone]
public partial class FileStationDelete
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; set; } = [];

    [JsonPropertyName("accurate_progress")]
    public bool? AccurateProgress { get; set; } = false;

    [JsonPropertyName("recursive")]
    public bool? Recursive { get; set; } = true;
}
