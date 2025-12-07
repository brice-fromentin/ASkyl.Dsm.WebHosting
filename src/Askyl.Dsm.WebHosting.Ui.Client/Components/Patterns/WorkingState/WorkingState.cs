namespace Askyl.Dsm.WebHosting.Ui.Components.Patterns.WorkingState;

public sealed class WorkingState : IDisposable
{
    private readonly IWorkingState _component;
    private readonly Action _stopWorking;
    private readonly Action _stateChanged;
    private bool _disposed;

    private WorkingState(IWorkingState component, Action startWorking, Action stopWorking, Action stateChanged)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(startWorking);
        ArgumentNullException.ThrowIfNull(stopWorking);
        ArgumentNullException.ThrowIfNull(stateChanged);

        _component = component;
        _stopWorking = stopWorking;
        _stateChanged = stateChanged;

        startWorking.Invoke();
        _stateChanged.Invoke();
    }

    public void UpdateMessage(string message)
    {
        if (_disposed)
        {
            return;
        }

        _component.Message = message;
        _stateChanged.Invoke();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _stopWorking();
        _stateChanged.Invoke();
        _disposed = true;
    }

    public static WorkingState Create(IWorkingState component, string message)
    {
        return new WorkingState(
            component: component,
            startWorking: () =>
            {
                component.IsWorking = true;
                component.Message = message;
            },
            stopWorking: () => component.IsWorking = false,
            stateChanged: component.NotifyStateChanged
        );
    }
}
