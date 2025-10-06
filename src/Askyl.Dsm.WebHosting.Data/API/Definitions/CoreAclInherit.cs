using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class CoreAclInherit
{
    [JsonPropertyName("child_files")]
    public bool ChildFiles { get; set; }

    [JsonPropertyName("child_folders")]
    public bool ChildFolders { get; set; }

    [JsonPropertyName("this_folder")]
    public bool ThisFolder { get; set; }

    [JsonPropertyName("all_descendants")]
    public bool AllDescendants { get; set; }
}
