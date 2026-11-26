using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;

public record CoreAclGet
{
    [JsonPropertyName("file_path")]
    public string FilePath { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("include_noname_rules")]
    public bool IncludeNonameRules { get; init; }
}
