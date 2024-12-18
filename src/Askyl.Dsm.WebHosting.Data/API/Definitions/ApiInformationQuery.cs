using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class ApiInformationQuery : IGenericCloneable<ApiInformationQuery>
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = DsmDefaults.RequiredApisJoined;

    public ApiInformationQuery Clone()
        => new () { Query = this.Query };
}
