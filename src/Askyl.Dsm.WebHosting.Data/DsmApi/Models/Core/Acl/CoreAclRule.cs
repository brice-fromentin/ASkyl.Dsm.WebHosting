using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;

public record CoreAclRule
{
    [JsonPropertyName("owner_type")]
    public string OwnerType { get; init; } = "";

    [JsonPropertyName("owner_name")]
    public string OwnerName { get; init; } = "";

    /// <summary>
    /// Sent back as DSM's <c>get</c> returned it: its own editor keeps it on the entries it rewrites.
    /// </summary>
    [JsonPropertyName("owner_id")]
    public int? OwnerId { get; init; }

    /// <summary>
    /// Only in a <c>get</c> answer: 0 for an entry set on the path, more for one inherited. Never sent to <c>set</c>.
    /// </summary>
    [JsonPropertyName("level")]
    public int? Level { get; init; }

    [JsonPropertyName("permission_type")]
    public string PermissionType { get; init; } = "";

    [JsonPropertyName("permission")]
    public CoreAclPermission Permission { get; init; } = new();

    [JsonPropertyName("inherit")]
    public CoreAclInherit Inherit { get; init; } = new();
}
