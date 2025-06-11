using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationBackgroundTask : IGenericCloneable<FileStationBackgroundTask>
{
    [JsonPropertyName("taskid")]
    public string? TaskId { get; set; }

    [JsonPropertyName("offset")]
    public int? Offset { get; set; } = 0;

    [JsonPropertyName("limit")]
    public int? Limit { get; set; } = 100;

    public FileStationBackgroundTask Clone()
        => new()
        {
            TaskId = this.TaskId,
            Offset = this.Offset,
            Limit = this.Limit
        };
}
