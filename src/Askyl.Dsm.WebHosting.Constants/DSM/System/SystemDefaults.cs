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
    public const string SynoInfoConfPath = "/etc/synoinfo.conf";

    /// <summary>
    /// Path to DSM's own web server configuration, which holds the HTTPS port (absent off a NAS).
    /// </summary>
    public const string DsmWebConfigPath = "/usr/syno/etc/www/DSM.json";

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

    /// <summary>
    /// Key name for system language in synoinfo.conf (e.g. "def" for default browser language, or a specific code).
    /// If "def", the browser language should be used as fallback.
    /// </summary>
    public const string KeyLanguage = "language";

    /// <summary>
    /// Object holding the HTTPS settings in DSM.json.
    /// </summary>
    public const string KeyDsmWebConfigSsl = "ssl";

    /// <summary>
    /// Port property in DSM.json.
    /// </summary>
    public const string KeyDsmWebConfigPort = "port";

    #endregion

    #region Loopback Mode

    /// <summary>
    /// Configuration key overriding <see cref="DsmWebConfigPath"/>.
    /// </summary>
    public const string ConfigKeyWebConfigPath = "DsmSettings:WebConfigPath";

    /// <summary>
    /// Address of DSM on the NAS itself, where nothing on the network can intercept the traffic.
    /// </summary>
    public const string LoopbackAddress = "127.0.0.1";

    /// <summary>
    /// Kernel table of IPv4 TCP sockets, readable by any user.
    /// </summary>
    public const string ProcNetTcpPath = "/proc/net/tcp";

    /// <summary>
    /// Kernel table of IPv6 TCP sockets, readable by any user.
    /// </summary>
    public const string ProcNetTcp6Path = "/proc/net/tcp6";

    /// <summary>
    /// State column of a listening socket in /proc/net/tcp (hexadecimal TCP_LISTEN).
    /// </summary>
    public const string ProcNetListenState = "0A";

    #endregion

    #region Default Values

    /// <summary>
    /// Default DSM HTTPS port when not configured otherwise.
    /// </summary>
    public const int DefaultHttpsPort = 5001;

    /// <summary>
    /// Value for language field in synoinfo.conf when browser language should be used.
    /// </summary>
    public const string DefaultLanguage = "def";

    #endregion
}
