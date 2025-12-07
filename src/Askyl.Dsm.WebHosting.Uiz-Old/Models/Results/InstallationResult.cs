namespace Askyl.Dsm.WebHosting.Ui.Models.Results;

public class InstallationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = String.Empty;

    public static InstallationResult CreateSuccess(string message = "Installation completed successfully.")
        => new() { Success = true, Message = message };

    public static InstallationResult CreateFailure(string message)
        => new() { Success = false, Message = message };
}
