using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Responses;

public class SynoLoginResponse : ApiResponseBase<SynoLogin>
{
}

public class SynoLogin
{
    [JsonPropertyName("sid")]
    public string Sid { get; set; } = String.Empty;
}
