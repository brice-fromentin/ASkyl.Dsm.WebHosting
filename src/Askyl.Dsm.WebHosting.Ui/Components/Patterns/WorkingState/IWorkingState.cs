namespace Askyl.Dsm.WebHosting.Ui.Components.Patterns.WorkingState;

public interface IWorkingState
{
    bool IsWorking { get; set; }
    string? WorkingMessage { get; set; }
    void NotifyStateChanged();
    string GetContextualMessage() => "Working...";
}
