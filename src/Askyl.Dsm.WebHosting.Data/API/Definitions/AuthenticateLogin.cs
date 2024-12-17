using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.Attributes;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class AuthenticateLogin
{
    [JsonPropertyName("account")]
    public string Account { get; set; } = default!;

    [JsonPropertyName("passwd")]
    [UrlEncode]
    public string Password { get; set; } = default!;

    [JsonPropertyName("format")]
    public string Format { get; } = "cookie";

    [JsonPropertyName("otp_code")]
    public string? OtpCode { get; set; }
}
