using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.Network;

namespace Askyl.Dsm.WebHosting.Data.Domain.WebSites;

public sealed class WebSiteConfiguration
{
    #region General

    /// <summary>
    /// Gets or sets the unique identifier for this configuration.
    /// Defaults to Guid.Empty for new configurations until persisted.
    /// </summary>
    public Guid Id { get; set; } = Guid.Empty;

    public string Name { get; set; } = "";

    #endregion

    #region Application

    public string ApplicationPath { get; set; } = "";

    public string ApplicationRealPath { get; set; } = "";

    public int InternalPort { get; set; } = WebSiteConstants.MinWebApplicationPort;

    public string Environment { get; set; } = WebSiteConstants.DefaultEnvironment;

    public bool IsEnabled { get; set; } = true;

    public bool AutoStart { get; set; } = true;

    /// <summary>
    /// Graceful shutdown timeout in seconds. Defaults to <c>WebSiteConstants.DefaultProcessTimeoutSeconds</c> (10s).
    /// </summary>
    public int ProcessTimeoutSeconds { get; set; } = WebSiteConstants.DefaultProcessTimeoutSeconds;

    public Dictionary<string, string> AdditionalEnvironmentVariables { get; init; } = [];

    #endregion

    #region Reverse Proxy

    public string HostName { get; set; } = "";

    public int PublicPort { get; set; } = WebSiteConstants.DefaultPublicPort;

    public ProtocolType Protocol { get; set; } = ProtocolType.HTTPS;

    public bool EnableHSTS { get; set; } = true;

    #endregion
}
