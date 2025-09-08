namespace Askyl.Dsm.WebHosting.Ui.Components.Patterns.WorkingState;

public sealed class WorkingState : IDisposable
{
    private readonly Action _stopWorking;
    private readonly Action _stateChanged;
    private bool _disposed;

    private WorkingState(Action startWorking, Action stopWorking, Action stateChanged)
    {
        ArgumentNullException.ThrowIfNull(startWorking);
        ArgumentNullException.ThrowIfNull(stopWorking);
        ArgumentNullException.ThrowIfNull(stateChanged);
        
        _stopWorking = stopWorking;
        _stateChanged = stateChanged;

        startWorking.Invoke();
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
