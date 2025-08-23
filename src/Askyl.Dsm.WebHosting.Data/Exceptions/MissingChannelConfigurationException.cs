namespace Askyl.Dsm.WebHosting.Data.Exceptions;

/// <summary>
/// Exception thrown when a required channel configuration is missing during critical operations.
/// This typically occurs when attempting channel-sensitive operations without proper configuration.
/// </summary>
public sealed class MissingChannelConfigurationException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingChannelConfigurationException"/> class.
    /// </summary>
    public MissingChannelConfigurationException()
        : base("Configured ChannelVersion is missing; aborting operation to avoid accidental removal of the last release.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingChannelConfigurationException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    public MissingChannelConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingChannelConfigurationException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MissingChannelConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
