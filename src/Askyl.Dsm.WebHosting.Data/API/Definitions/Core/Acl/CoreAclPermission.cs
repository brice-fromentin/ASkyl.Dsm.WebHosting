using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class CoreAclPermission
{
    [JsonPropertyName("read_data")]
    public bool ReadData { get; set; }

    [JsonPropertyName("write_data")]
    public bool WriteData { get; set; }

    [JsonPropertyName("exe_file")]
    public bool ExeFile { get; set; }

    [JsonPropertyName("append_data")]
    public bool AppendData { get; set; }

    [JsonPropertyName("delete")]
    public bool Delete { get; set; }

    [JsonPropertyName("delete_sub")]
    public bool DeleteSub { get; set; }

    [JsonPropertyName("read_attr")]
    public bool ReadAttr { get; set; }

    [JsonPropertyName("write_attr")]
    public bool WriteAttr { get; set; }

    [JsonPropertyName("read_ext_attr")]
    public bool ReadExtAttr { get; set; }

    [JsonPropertyName("write_ext_attr")]
    public bool WriteExtAttr { get; set; }

    [JsonPropertyName("read_perm")]
    public bool ReadPerm { get; set; }

    [JsonPropertyName("change_perm")]
    public bool ChangePerm { get; set; }

    [JsonPropertyName("take_ownership")]
    public bool TakeOwnership { get; set; }
}
