namespace Askyl.Dsm.WebHosting.Ui.Components.Patterns.WorkingState;

/// <summary>
/// Extension methods for IWorkingState to create WorkingState instances directly
/// </summary>
public static class IWorkingStateExtensions
{
    public static WorkingState CreateWorkingState(this IWorkingState component, string message)
    {
        return WorkingState.Create(component, message);
    }
}
