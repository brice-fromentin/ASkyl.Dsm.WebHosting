using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationTime : IGenericCloneable<FileStationTime>
{
    [JsonPropertyName("atime")]
    public long? AccessTime { get; set; }

    [JsonPropertyName("mtime")]
    public long? ModifyTime { get; set; }

    [JsonPropertyName("ctime")]
    public long? CreateTime { get; set; }

    [JsonPropertyName("crtime")]
    public long? CreationTime { get; set; }

    public FileStationTime Clone()
        => new()
        {
            AccessTime = this.AccessTime,
            ModifyTime = this.ModifyTime,
            CreateTime = this.CreateTime,
            CreationTime = this.CreationTime
        };
}
