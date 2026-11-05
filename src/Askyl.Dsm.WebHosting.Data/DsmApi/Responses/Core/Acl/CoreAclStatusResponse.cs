using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.Acl;

public class CoreAclStatusResponse : ApiResponseBase<CoreAclStatusData>
{
}

public class CoreAclStatusData
{
    [JsonPropertyName("finished")]
    public bool Finished { get; set; }
}
