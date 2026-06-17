using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using FluentValidation;

namespace Askyl.Dsm.WebHosting.Globalization.Validators;

public sealed class WebSiteConfigurationValidator : AbstractValidator<WebSiteConfiguration>
{
    public WebSiteConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithLocalizedMessage(L.WebSiteConfiguration.NameRequired)
            .Length(1, 100).WithLocalizedMessage(L.WebSiteConfiguration.NameLength);

        RuleFor(x => x.ApplicationPath)
            .NotEmpty().WithLocalizedMessage(L.WebSiteConfiguration.ApplicationPathRequired);

        RuleFor(x => x.InternalPort)
            .GreaterThan(0).WithLocalizedMessage(L.WebSiteConfiguration.InternalPortRequired)
            .InclusiveBetween(WebSiteConstants.MinWebApplicationPort, WebSiteConstants.MaxWebApplicationPort)
            .WithLocalizedMessage(L.WebSiteConfiguration.InternalPortRange);

        RuleFor(x => x.Environment)
            .NotEmpty().WithLocalizedMessage(L.WebSiteConfiguration.EnvironmentRequired);

        RuleFor(x => x.ProcessTimeoutSeconds)
            .InclusiveBetween(WebSiteConstants.MinProcessTimeoutSeconds, WebSiteConstants.MaxProcessTimeoutSeconds)
            .WithLocalizedMessage(L.WebSiteConfiguration.ProcessTimeoutRange);

        RuleFor(x => x.HostName)
            .NotEmpty().WithLocalizedMessage(L.WebSiteConfiguration.HostNameRequired);

        RuleFor(x => x.PublicPort)
            .GreaterThan(0).WithLocalizedMessage(L.WebSiteConfiguration.PublicPortRequired)
            .Must(port => WebSiteConstants.WellKnownWebPorts.Contains(port) || port is >= WebSiteConstants.MinWebApplicationPort and <= WebSiteConstants.MaxWebApplicationPort)
            .WithLocalizedMessage(L.WebSiteConfiguration.PublicPortRange);
    }
}
