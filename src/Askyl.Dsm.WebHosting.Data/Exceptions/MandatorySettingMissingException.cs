namespace Askyl.Dsm.WebHosting.Data.Exceptions;

/// <summary>
/// Thrown when a mandatory DSM configuration setting is missing or empty.
/// </summary>
public sealed class MandatorySettingMissingException : Exception
{
    public string? SettingKey { get; }

    public MandatorySettingMissingException(string settingKey)
        : base($"Mandatory setting '{settingKey}' is missing or empty.")
    {
        SettingKey = settingKey;
    }

    public MandatorySettingMissingException(string settingKey, Exception innerException)
        : base($"Mandatory setting '{settingKey}' is missing or empty.", innerException)
    {
        SettingKey = settingKey;
    }

    public MandatorySettingMissingException() : base()
    {
    }
}
