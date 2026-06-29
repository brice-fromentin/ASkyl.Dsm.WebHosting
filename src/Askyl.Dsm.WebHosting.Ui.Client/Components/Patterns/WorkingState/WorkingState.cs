namespace Askyl.Dsm.WebHosting.Ui.Client.Components.Patterns.WorkingState;

public sealed class WorkingState : IDisposable
{
    private readonly WorkingStateBase _component;
    private readonly Action _stopWorking;
    private bool _disposed;

    private WorkingState(WorkingStateBase component, Action startWorking, Action stopWorking)
    {
        _component = component;
        _stopWorking = stopWorking;
        startWorking();
        component.NotifyStateChanged();
    }

    public void UpdateMessage(string message)
    {
        if (_disposed)
        {
            return;
        }

        _component.Message = message;
        _component.NotifyStateChanged();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _stopWorking();
        _component.NotifyStateChanged();
        _disposed = true;
    }

    public static WorkingState Create(WorkingStateBase component, string message)
    {
        return new WorkingState(
            component: component,
            startWorking: () =>
            {
                component.IsWorking = true;
                component.Message = message;
            },
            stopWorking: () => component.IsWorking = false
        );
    }
}
