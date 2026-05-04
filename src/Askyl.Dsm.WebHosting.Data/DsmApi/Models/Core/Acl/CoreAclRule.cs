using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;

public record CoreAclRule
{
    [JsonPropertyName("owner_type")]
    public string OwnerType { get; init; } = "";

    [JsonPropertyName("owner_name")]
    public string OwnerName { get; init; } = "";

    [JsonPropertyName("permission_type")]
    public string PermissionType { get; init; } = "";

    [JsonPropertyName("permission")]
    public CoreAclPermission Permission { get; init; } = new();

    [JsonPropertyName("inherit")]
    public CoreAclInherit Inherit { get; init; } = new();
}
