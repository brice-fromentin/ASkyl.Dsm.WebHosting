using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;

public record CoreAclPermission
{
    [JsonPropertyName("read_data")]
    public bool ReadData { get; init; }

    [JsonPropertyName("write_data")]
    public bool WriteData { get; init; }

    [JsonPropertyName("exe_file")]
    public bool ExeFile { get; init; }

    [JsonPropertyName("append_data")]
    public bool AppendData { get; init; }

    [JsonPropertyName("delete")]
    public bool Delete { get; init; }

    [JsonPropertyName("delete_sub")]
    public bool DeleteSub { get; init; }

    [JsonPropertyName("read_attr")]
    public bool ReadAttr { get; init; }

    [JsonPropertyName("write_attr")]
    public bool WriteAttr { get; init; }

    [JsonPropertyName("read_ext_attr")]
    public bool ReadExtAttr { get; init; }

    [JsonPropertyName("write_ext_attr")]
    public bool WriteExtAttr { get; init; }

    [JsonPropertyName("read_perm")]
    public bool ReadPerm { get; init; }

    [JsonPropertyName("change_perm")]
    public bool ChangePerm { get; init; }

    [JsonPropertyName("take_ownership")]
    public bool TakeOwnership { get; init; }
}
