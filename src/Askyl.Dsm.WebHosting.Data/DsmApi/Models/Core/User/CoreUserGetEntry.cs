using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.User;

public record CoreUserGetEntry
{
    public CoreUserGetEntry(string name)
    {
        Name = name;
    }

    public CoreUserGetEntry()
    {
    }

    [JsonPropertyName("name")]
    public string Name { get; init; } = default!;
}
