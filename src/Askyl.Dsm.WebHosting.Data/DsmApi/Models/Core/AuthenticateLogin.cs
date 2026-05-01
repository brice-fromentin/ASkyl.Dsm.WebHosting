using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

public record AuthenticateLogin
{
    [JsonPropertyName("account")]
    public string Account { get; init; } = default!;

    [JsonPropertyName("passwd")]
    public string Password { get; init; } = default!;

    [JsonPropertyName("format")]
    public string Format { get; } = "cookie";

    [JsonPropertyName("otp_code")]
    public string? OtpCode { get; init; }
}
