using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

[GenerateClone]
public partial class FileStationDirSize
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; set; } = [];
}
