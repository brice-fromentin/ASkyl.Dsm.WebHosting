namespace Askyl.Dsm.WebHosting.Constants.DSM.System;

/// <summary>
/// Defines DSM system-level configuration defaults and constants.
/// </summary>
public static class SystemDefaults
{
    #region Configuration File Paths

    /// <summary>
    /// Path to Synology system configuration file.
    /// </summary>
    public const string ConfigurationFileName = "/etc/synoinfo.conf";

    #endregion

    #region Configuration Keys

    /// <summary>
    /// Key name for external host IP in synoinfo.conf.
    /// </summary>
    public const string KeyExternalHostIp = "external_host_ip";

    /// <summary>
    /// Key name for external HTTPS port in synoinfo.conf.
    /// </summary>
    public const string KeyExternalHttpsPort = "external_port_dsm_https";

    #endregion

    #region Default Values

    /// <summary>
    /// Default DSM HTTPS port when not configured otherwise.
    /// </summary>
    public const int DefaultHttpsPort = 5001;

    #endregion
}
