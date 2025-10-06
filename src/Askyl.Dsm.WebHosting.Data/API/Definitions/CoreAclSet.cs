using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class CoreAclSet
{
    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = "";

    [JsonPropertyName("files")]
    public string Files { get; set; } = "";

    [JsonPropertyName("dirPaths")]
    public string DirPaths { get; set; } = "";

    [JsonPropertyName("change_acl")]
    public bool ChangeAcl { get; set; }

    [JsonPropertyName("rules")]
    public List<CoreAclRule> Rules { get; set; } = [];

    [JsonPropertyName("inherited")]
    public bool Inherited { get; set; }

    [JsonPropertyName("acl_recur")]
    public bool AclRecur { get; set; }
}
