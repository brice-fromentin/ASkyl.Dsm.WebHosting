using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;

public record CoreAclSet
{
    [JsonPropertyName("file_path")]
    public string FilePath { get; init; } = "";

    [JsonPropertyName("files")]
    public string Files { get; init; } = "";

    [JsonPropertyName("dirPaths")]
    public string DirPaths { get; init; } = "";

    [JsonPropertyName("change_acl")]
    public bool ChangeAcl { get; init; }

    [JsonPropertyName("rules")]
    public List<CoreAclRule> Rules { get; init; } = [];

    [JsonPropertyName("inherited")]
    public bool Inherited { get; init; }

    [JsonPropertyName("acl_recur")]
    public bool AclRecur { get; init; }
}
