using System.ComponentModel.DataAnnotations;

namespace Askyl.Dsm.WebHosting.Data.Security;

public class LoginModel
{
    [Required]
    public string Login { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";

    public string? OtpCode { get; set; }

}
