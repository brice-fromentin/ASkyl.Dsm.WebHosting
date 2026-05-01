using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationTime
{
    [JsonPropertyName("atime")]
    public long? AccessTime { get; init; }

    [JsonPropertyName("mtime")]
    public long? ModifyTime { get; init; }

    [JsonPropertyName("ctime")]
    public long? CreateTime { get; init; }

    [JsonPropertyName("crtime")]
    public long? CreationTime { get; init; }
}
