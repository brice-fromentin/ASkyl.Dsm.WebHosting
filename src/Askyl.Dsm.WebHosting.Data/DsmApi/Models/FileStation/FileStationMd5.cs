using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

[GenerateClone]
public partial class FileStationMd5
{
    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = default!;
}
