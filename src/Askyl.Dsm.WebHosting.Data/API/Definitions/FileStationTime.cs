using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class FileStationTime
{
    [JsonPropertyName("atime")]
    public long? AccessTime { get; set; }

    [JsonPropertyName("mtime")]
    public long? ModifyTime { get; set; }

    [JsonPropertyName("ctime")]
    public long? CreateTime { get; set; }

    [JsonPropertyName("crtime")]
    public long? CreationTime { get; set; }
}
