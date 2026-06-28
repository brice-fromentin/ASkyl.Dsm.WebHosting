using Microsoft.AspNetCore.Components;

namespace Askyl.Dsm.WebHosting.Ui.Client.Components.Patterns.WorkingState;

public abstract class WorkingStateBase : ComponentBase, IWorkingState
{
    public bool IsWorking { get; set; }

    public string Message { get; set; } = String.Empty;

    public abstract void NotifyStateChanged();
}
