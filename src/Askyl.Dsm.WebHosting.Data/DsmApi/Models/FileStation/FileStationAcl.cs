using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationAcl
{
    [JsonPropertyName("append")]
    public bool? Append { get; init; }

    [JsonPropertyName("del")]
    public bool? Delete { get; init; }

    [JsonPropertyName("exec")]
    public bool? Execute { get; init; }

    [JsonPropertyName("read")]
    public bool? Read { get; init; }

    [JsonPropertyName("write")]
    public bool? Write { get; init; }
}
