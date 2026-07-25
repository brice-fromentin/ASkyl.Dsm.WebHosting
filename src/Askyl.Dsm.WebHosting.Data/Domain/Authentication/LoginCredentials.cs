namespace Askyl.Dsm.WebHosting.Data.Domain.Authentication;

public class LoginCredentials(string login, string password, string? otpCode)
{
    /// <summary>
    /// Gets or sets the login username.
    /// NOTE: Remains mutable (get; set;) because Blazor @bind-Value in Login.razor
    /// requires a set accessor for two-way form binding.
    /// </summary>
    public string Login { get; set; } = login;

    /// <summary>
    /// Gets or sets the password.
    /// NOTE: Remains mutable (get; set;) because Blazor @bind-Value in Login.razor
    /// requires a set accessor for two-way form binding. Making this `init` would
    /// break the login form. Consider refactoring Login.razor to use individual
    /// @code fields if credential immutability is required.
    /// </summary>
    public string Password { get; set; } = password;

    /// <summary>
    /// Gets or sets the optional one-time password code.
    /// NOTE: Remains mutable (get; set;) because Blazor @bind-Value in Login.razor
    /// requires a set accessor for two-way form binding.
    /// </summary>
    public string? OtpCode { get; set; } = otpCode;

    // Parameterless constructor for Razor page binding
    public LoginCredentials() : this(String.Empty, String.Empty, null) { }
}
