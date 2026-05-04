using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants.DSM.API;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

public record ApiInformationQuery
{
    [JsonPropertyName("query")]
    public string Query { get; init; } = ApiNames.RequiredApisJoined;
}
