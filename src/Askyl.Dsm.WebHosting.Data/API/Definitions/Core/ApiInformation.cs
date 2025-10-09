using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

public class ApiInformation
{
    [JsonPropertyName("maxVersion")]
    public int MaxVersion { get; set; }

    [JsonPropertyName("minVersion")]
    public int MinVersion { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = String.Empty;
}
