using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Responses;

public class CoreAclSetResponse : ApiResponseBase<CoreAclSetData>
{
}

public class CoreAclSetData
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = "";
}
