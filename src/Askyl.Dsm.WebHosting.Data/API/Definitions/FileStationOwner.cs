using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class FileStationOwner
{
    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("group")]
    public string? Group { get; set; }

    [JsonPropertyName("uid")]
    public int? Uid { get; set; }

    [JsonPropertyName("gid")]
    public int? Gid { get; set; }
}
