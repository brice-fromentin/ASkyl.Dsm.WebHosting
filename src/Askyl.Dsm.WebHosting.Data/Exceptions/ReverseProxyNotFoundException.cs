namespace Askyl.Dsm.WebHosting.Data.Exceptions;

/// <summary>
/// Exception thrown when a reverse proxy is not found during update operations.
/// Signals that recovery mechanisms should be attempted or the proxy was externally deleted.
/// </summary>
public class ReverseProxyNotFoundException : Exception
{
    /// <summary>
    /// Creates a new instance with a custom message.
    /// </summary>
    public ReverseProxyNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance with a message and inner exception.
    /// </summary>
    public ReverseProxyNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
