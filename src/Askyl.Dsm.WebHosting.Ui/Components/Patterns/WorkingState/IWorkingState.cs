namespace Askyl.Dsm.WebHosting.Ui.Components.Patterns.WorkingState;

public interface IWorkingState
{
    bool IsWorking { get; set; }
    string Message { get; set; }
    void NotifyStateChanged();
}
