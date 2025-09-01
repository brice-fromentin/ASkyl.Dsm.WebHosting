using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.Attributes;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class AuthenticateLogin : IGenericCloneable<AuthenticateLogin>
{
    [JsonPropertyName("account")]
    public string Account { get; set; } = default!;

    [JsonPropertyName("passwd")]
    public string Password { get; set; } = default!;

    [JsonPropertyName("format")]
    public string Format { get; } = AuthenticationDefaults.FormatCookie;

    [JsonPropertyName("otp_code")]
    public string? OtpCode { get; set; }

    public AuthenticateLogin Clone()
        => new() { Account = this.Account, Password = this.Password, OtpCode = this.OtpCode };
}
