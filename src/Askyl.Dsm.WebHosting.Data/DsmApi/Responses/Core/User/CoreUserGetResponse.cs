using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.User;

/// <summary>
/// Response for SYNO.Core.User.get — returns user details for a specific username.
/// Actual response: {"data":{"users":[{"name":"brice","uid":1026}]},"success":true}
/// </summary>
public class CoreUserGetResponse : ApiResponseBase<CoreUserGetData>
{
}

public class CoreUserGetData
{
    [JsonPropertyName("users")]
    public List<CoreUserGetUser> Users { get; set; } = [];
}

public class CoreUserGetUser
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("uid")]
    public int Uid { get; set; }
}
