using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class FileStationMd5
{
    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = default!;
}
