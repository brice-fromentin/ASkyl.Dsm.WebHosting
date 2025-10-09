using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

[GenerateClone]
public partial class ApiInformationQuery
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = DsmApiNames.RequiredApisJoined;
}
