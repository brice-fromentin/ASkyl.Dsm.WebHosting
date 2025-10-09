using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

[GenerateClone]
public partial class FileStationPermission
{
    [JsonPropertyName("posix")]
    public int? Posix { get; set; }

    [JsonPropertyName("is_acl_mode")]
    public bool? IsAclMode { get; set; }

    [JsonPropertyName("acl")]
    public FileStationAcl? Acl { get; set; }
}
