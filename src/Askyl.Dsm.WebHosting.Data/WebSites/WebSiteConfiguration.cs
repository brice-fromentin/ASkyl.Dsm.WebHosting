using System.ComponentModel.DataAnnotations;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.WebSites;

[GenerateClone]
public partial class WebSiteConfiguration : IGenericCloneable<WebSiteConfiguration>
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = ApplicationConstants.SiteNameRequiredErrorMessage)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = ApplicationConstants.ApplicationPathRequiredErrorMessage)]
    public string ApplicationPath { get; set; } = "";

    public string ApplicationRealPath { get; set; } = "";

    [Required(ErrorMessage = ApplicationConstants.PortRequiredErrorMessage)]
    [Range(ApplicationConstants.MinWebApplicationPort, ApplicationConstants.MaxWebApplicationPort, ErrorMessage = ApplicationConstants.PortRangeErrorMessage)]
    public int Port { get; set; }

    [Required(ErrorMessage = ApplicationConstants.EnvironmentRequiredErrorMessage)]
    public string Environment { get; set; } = ApplicationConstants.DefaultEnvironment;

    public bool IsEnabled { get; set; } = true;

    public bool AutoStart { get; set; } = true;

    public Dictionary<string, string> AdditionalEnvironmentVariables { get; set; } = [];
}