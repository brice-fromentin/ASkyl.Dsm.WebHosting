using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class ReverseProxyCustomHeader : IGenericCloneable<ReverseProxyCustomHeader>
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;

    public ReverseProxyCustomHeader Clone()
        => new() { Name = this.Name, Value = this.Value };
}
