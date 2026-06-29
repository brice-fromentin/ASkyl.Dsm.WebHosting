namespace Askyl.Dsm.WebHosting.Ui.Client.Components.Patterns.WorkingState;

/// <summary>
/// Extension methods for WorkingStateBase to create WorkingState instances directly.
/// </summary>
public static class WorkingStateExtensions
{
    public static WorkingState CreateWorkingState(this WorkingStateBase component, string message)
    {
        return WorkingState.Create(component, message);
    }
}
