using System.ComponentModel.DataAnnotations;

namespace Askyl.Dsm.WebHosting.Data.Security;

public class LoginModel(string login, string password, string? otpCode)
{
    [Required(ErrorMessage = "Login is required.")]
    public string Login { get; set; } = login;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = password;

    public string? OtpCode { get; set; } = otpCode;

    // Parameterless constructor for Razor page binding
    public LoginModel() : this(String.Empty, String.Empty, null) { }
}
