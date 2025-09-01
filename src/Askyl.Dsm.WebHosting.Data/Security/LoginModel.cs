using System.ComponentModel.DataAnnotations;

namespace Askyl.Dsm.WebHosting.Data.Security;

public class LoginModel
{
    [Required(ErrorMessage = "Login is required.")]
    public string Login { get; set; } = String.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = String.Empty;

    public string? OtpCode { get; set; }

}
