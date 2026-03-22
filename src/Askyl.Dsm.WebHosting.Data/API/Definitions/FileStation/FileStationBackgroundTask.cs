using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

[GenerateClone]
public partial class FileStationBackgroundTask
{
    [JsonPropertyName("taskid")]
    public string? TaskId { get; set; }

    [JsonPropertyName("offset")]
    public int? Offset { get; set; } = 0;

    [JsonPropertyName("limit")]
    public int? Limit { get; set; } = 100;
}
