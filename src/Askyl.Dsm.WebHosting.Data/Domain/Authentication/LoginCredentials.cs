namespace Askyl.Dsm.WebHosting.Data.Domain.Authentication;

public class LoginCredentials(string login, string password, string? otpCode)
{
    public string Login { get; set; } = login;

    public string Password { get; set; } = password;

    public string? OtpCode { get; set; } = otpCode;

    // Parameterless constructor for Razor page binding
    public LoginCredentials() : this(String.Empty, String.Empty, null) { }
}
