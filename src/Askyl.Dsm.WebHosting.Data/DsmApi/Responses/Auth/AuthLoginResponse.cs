using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Auth;

public class AuthLoginResponse : ApiResponseBase<AuthLogin>
{
}

public class AuthLogin
{
    [JsonPropertyName("sid")]
    public string Sid { get; set; } = String.Empty;
}
