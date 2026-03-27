using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

[GenerateClone]
public partial class ApiInformationQuery
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = ApiNames.RequiredApisJoined;
}
