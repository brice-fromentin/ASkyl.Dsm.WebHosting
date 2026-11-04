using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.Acl;

public class CoreAclGetResponse : ApiResponseBase<CoreAclGetData>
{
}

public class CoreAclGetData
{
    [JsonPropertyName("acl")]
    public List<CoreAclRule> Acl { get; set; } = [];

    [JsonPropertyName("is_inherited")]
    public bool IsInherited { get; set; }
}
