using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;

public record CoreAclInherit
{
    [JsonPropertyName("child_files")]
    public bool ChildFiles { get; init; }

    [JsonPropertyName("child_folders")]
    public bool ChildFolders { get; init; }

    [JsonPropertyName("this_folder")]
    public bool ThisFolder { get; init; }

    [JsonPropertyName("all_descendants")]
    public bool AllDescendants { get; init; }
}
