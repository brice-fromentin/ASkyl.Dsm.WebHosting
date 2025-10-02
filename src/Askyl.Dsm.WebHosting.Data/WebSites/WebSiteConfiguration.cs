using System.ComponentModel.DataAnnotations;
using Askyl.Dsm.WebHosting.Constants.Application;

namespace Askyl.Dsm.WebHosting.Data.WebSites;

public class WebSiteConfiguration
{
    [Required(ErrorMessage = ApplicationConstants.SiteNameRequiredErrorMessage)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = ApplicationConstants.ApplicationPathRequiredErrorMessage)]
    public string ApplicationPath { get; set; } = "";

    [Required(ErrorMessage = ApplicationConstants.PortRequiredErrorMessage)]
    [Range(ApplicationConstants.MinWebApplicationPort, ApplicationConstants.MaxWebApplicationPort, ErrorMessage = ApplicationConstants.PortRangeErrorMessage)]
    public int Port { get; set; }

    [Required(ErrorMessage = ApplicationConstants.EnvironmentRequiredErrorMessage)]
    public string Environment { get; set; } = ApplicationConstants.DefaultEnvironment;

    public bool IsEnabled { get; set; } = true;

    public bool AutoStart { get; set; } = true;

    public Dictionary<string, string> AdditionalEnvironmentVariables { get; set; } = [];
}