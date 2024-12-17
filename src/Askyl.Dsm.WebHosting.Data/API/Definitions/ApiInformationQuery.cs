using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class ApiInformationQuery
{
    [JsonPropertyName("query")]
    public string Query { get; } = DsmDefaults.RequiredApisJoined;
}
