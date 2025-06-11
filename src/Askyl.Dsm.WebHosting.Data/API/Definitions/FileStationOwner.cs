using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationOwner : IGenericCloneable<FileStationOwner>
{
    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("group")]
    public string? Group { get; set; }

    [JsonPropertyName("uid")]
    public int? Uid { get; set; }

    [JsonPropertyName("gid")]
    public int? Gid { get; set; }

    public FileStationOwner Clone()
        => new()
        {
            User = this.User,
            Group = this.Group,
            Uid = this.Uid,
            Gid = this.Gid
        };
}
