namespace Askyl.Dsm.WebHosting.Ui.Client.Components.Patterns.WorkingState;

public interface IWorkingState
{
    bool IsWorking { get; set; }
    string Message { get; set; }
    void NotifyStateChanged();
}
