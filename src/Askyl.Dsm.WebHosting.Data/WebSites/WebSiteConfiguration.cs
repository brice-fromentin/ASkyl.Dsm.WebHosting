using System.ComponentModel.DataAnnotations;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.UI;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.WebSites;

[GenerateClone]
public partial class WebSiteConfiguration : IGenericCloneable<WebSiteConfiguration>
{
    #region General

    public Guid Id { get; set; }

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

    public Guid? IdReverseProxy { get; set; }

    public string? HostName { get; set; }

    [Range(ApplicationConstants.MinWebApplicationPort, ApplicationConstants.MaxWebApplicationPort, ErrorMessage = ApplicationConstants.PortRangeErrorMessage)]
    public int PublicPort { get; set; }

    public ProtocolType Protocol { get; set; } = ProtocolType.HTTPS;

    public bool EnableHSTS { get; set; } = true;

    #endregion
}