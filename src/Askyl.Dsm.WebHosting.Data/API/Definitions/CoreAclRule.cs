using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class CoreAclRule
{
    [JsonPropertyName("owner_type")]
    public string OwnerType { get; set; } = "";

    [JsonPropertyName("owner_name")]
    public string OwnerName { get; set; } = "";

    [JsonPropertyName("permission_type")]
    public string PermissionType { get; set; } = "";

    [JsonPropertyName("permission")]
    public CoreAclPermission Permission { get; set; } = new();

    [JsonPropertyName("inherit")]
    public CoreAclInherit Inherit { get; set; } = new();
}
