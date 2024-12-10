using System;
using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API;

public class SynoLoginResponse : ApiResponse<SynoLogin>
{
}

public class SynoLogin
{
    [JsonPropertyName("sid")]
    public string Sid { get; set; } = "";
}
