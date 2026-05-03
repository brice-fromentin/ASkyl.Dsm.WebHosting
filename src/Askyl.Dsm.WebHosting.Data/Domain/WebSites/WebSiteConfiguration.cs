using System.ComponentModel.DataAnnotations;
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

    [Required(ErrorMessage = WebSiteConstants.SiteNameRequiredErrorMessage)]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Site name cannot be empty and must be 100 characters or less.")]
    public string Name { get; set; } = "";

    #endregion

    #region Application

    [Required(ErrorMessage = WebSiteConstants.ApplicationPathRequiredErrorMessage)]
    public string ApplicationPath { get; set; } = "";

    public string ApplicationRealPath { get; set; } = "";

    [Required(ErrorMessage = WebSiteConstants.PortRequiredErrorMessage)]
    [Range(WebSiteConstants.MinWebApplicationPort, WebSiteConstants.MaxWebApplicationPort, ErrorMessage = WebSiteConstants.PortRangeErrorMessage)]
    public int InternalPort { get; set; } = WebSiteConstants.MinWebApplicationPort;

    [Required(ErrorMessage = WebSiteConstants.EnvironmentRequiredErrorMessage)]
    public string Environment { get; set; } = WebSiteConstants.DefaultEnvironment;

    public bool IsEnabled { get; set; } = true;

    public bool AutoStart { get; set; } = true;

    /// <summary>
    /// Graceful shutdown timeout in seconds. Defaults to <c>WebSiteConstants.DefaultProcessTimeoutSeconds</c> (10s).
    /// </summary>
    [Range(WebSiteConstants.MinProcessTimeoutSeconds, WebSiteConstants.MaxProcessTimeoutSeconds, ErrorMessage = WebSiteConstants.ProcessTimeoutRangeErrorMessage)]
    public int ProcessTimeoutSeconds { get; set; } = WebSiteConstants.DefaultProcessTimeoutSeconds;

    public Dictionary<string, string> AdditionalEnvironmentVariables { get; set; } = [];

    #endregion

    #region Reverse Proxy

    [Required(ErrorMessage = WebSiteConstants.HostNameRequiredErrorMessage)]
    public string HostName { get; set; } = "";

    [Required(ErrorMessage = WebSiteConstants.PortRequiredErrorMessage)]
    public int PublicPort { get; set; } = 443;

    public ProtocolType Protocol { get; set; } = ProtocolType.HTTPS;

    public bool EnableHSTS { get; set; } = true;

    #endregion
}
