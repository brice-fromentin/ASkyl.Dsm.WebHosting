using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

[GenerateClone]
public partial class FileStationAcl
{
    [JsonPropertyName("append")]
    public bool? Append { get; set; }

    [JsonPropertyName("del")]
    public bool? Delete { get; set; }

    [JsonPropertyName("exec")]
    public bool? Execute { get; set; }

    [JsonPropertyName("read")]
    public bool? Read { get; set; }

    [JsonPropertyName("write")]
    public bool? Write { get; set; }
}
