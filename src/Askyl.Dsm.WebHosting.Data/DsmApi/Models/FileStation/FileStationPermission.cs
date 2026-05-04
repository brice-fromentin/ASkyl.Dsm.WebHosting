using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationPermission
{
    [JsonPropertyName("posix")]
    public int? Posix { get; init; }

    [JsonPropertyName("is_acl_mode")]
    public bool? IsAclMode { get; init; }

    [JsonPropertyName("acl")]
    public FileStationAcl? Acl { get; init; }
}
