using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationPermission : IGenericCloneable<FileStationPermission>
{
    [JsonPropertyName("posix")]
    public int? Posix { get; set; }

    [JsonPropertyName("is_acl_mode")]
    public bool? IsAclMode { get; set; }

    [JsonPropertyName("acl")]
    public FileStationAcl? Acl { get; set; }

    public FileStationPermission Clone()
        => new()
        {
            Posix = this.Posix,
            IsAclMode = this.IsAclMode,
            Acl = this.Acl?.Clone()
        };
}
