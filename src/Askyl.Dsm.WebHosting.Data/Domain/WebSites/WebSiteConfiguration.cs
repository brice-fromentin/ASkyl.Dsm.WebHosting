using System.ComponentModel.DataAnnotations;

using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.Patterns;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.Domain.WebSites;

[GenerateClone]
public partial class WebSiteConfiguration : IGenericCloneable<WebSiteConfiguration>
{
    #region General

    /// <summary>
    /// Gets or sets the unique identifier for this configuration.
    /// Defaults to Guid.Empty for new configurations until persisted.
    /// </summary>
    public Guid Id { get; set; } = Guid.Empty;

    [Required(ErrorMessage = ApplicationConstants.SiteNameRequiredErrorMessage)]
    public string Name { get; set; } = "";

    #endregion

    #region Application

    [Required(ErrorMessage = ApplicationConstants.ApplicationPathRequiredErrorMessage)]
    public string ApplicationPath { get; set; } = "";

    public string ApplicationRealPath { get; set; } = "";

    [Required(ErrorMessage = ApplicationConstants.PortRequiredErrorMessage)]
    [Range(ApplicationConstants.MinWebApplicationPort, ApplicationConstants.MaxWebApplicationPort, ErrorMessage = ApplicationConstants.PortRangeErrorMessage)]
    public int InternalPort { get; set; }

    [Required(ErrorMessage = ApplicationConstants.EnvironmentRequiredErrorMessage)]
    public string Environment { get; set; } = ApplicationConstants.DefaultEnvironment;

    public bool IsEnabled { get; set; } = true;

    public bool AutoStart { get; set; } = true;

    public Dictionary<string, string> AdditionalEnvironmentVariables { get; set; } = [];

    #endregion

    #region Reverse Proxy

    [Required(ErrorMessage = ApplicationConstants.HostNameRequiredErrorMessage)]
    public string HostName { get; set; } = "";

    [Required(ErrorMessage = ApplicationConstants.PortRequiredErrorMessage)]
    public int PublicPort { get; set; } = 443;

    public ProtocolType Protocol { get; set; } = ProtocolType.HTTPS;

    public bool EnableHSTS { get; set; } = true;

    #endregion
}