namespace Askyl.Dsm.WebHosting.Ui.Models.Results;

public record OperationResult(bool Success, string? ErrorMessage = null)
{
    public static OperationResult CreateSuccess() => new(true);

    public static OperationResult CreateFailure(string errorMessage) => new(false, errorMessage);
}
