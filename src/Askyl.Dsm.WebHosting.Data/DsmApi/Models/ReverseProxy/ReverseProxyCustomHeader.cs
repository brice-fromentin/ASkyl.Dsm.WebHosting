using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

public record ReverseProxyCustomHeader
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = default!;

    [JsonPropertyName("value")]
    public string Value { get; init; } = default!;
}
