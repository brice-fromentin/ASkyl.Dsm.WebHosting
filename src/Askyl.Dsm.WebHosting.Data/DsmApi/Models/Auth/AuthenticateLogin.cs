using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants.DSM.API;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.Auth;

public record AuthenticateLogin
{
    [JsonPropertyName("account")]
    public string Account { get; init; } = default!;

    [JsonPropertyName("passwd")]
    public string Password { get; init; } = default!;

    [JsonPropertyName("format")]
    public string Format { get; } = DsmConstants.AuthFormatCookie;

    [JsonPropertyName("otp_code")]
    public string? OtpCode { get; init; }

    public AuthenticateLogin()
    {
    }

    public AuthenticateLogin(string account, string password, string? otpCode = null)
    {
        Account = account;
        Password = password;
        OtpCode = otpCode;
    }
}
