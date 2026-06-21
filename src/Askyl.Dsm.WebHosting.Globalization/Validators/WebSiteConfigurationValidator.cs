using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using FluentValidation;

namespace Askyl.Dsm.WebHosting.Globalization.Validators;

public sealed class WebSiteConfigurationValidator : AbstractValidator<WebSiteConfiguration>
{
    public WebSiteConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithLocalizedMessage(LK.WebSiteConfiguration.NameRequired)
            .Length(1, 100).WithLocalizedMessage(LK.WebSiteConfiguration.NameLength);

        RuleFor(x => x.ApplicationPath)
            .NotEmpty().WithLocalizedMessage(LK.WebSiteConfiguration.ApplicationPathRequired);

        RuleFor(x => x.InternalPort)
            .GreaterThan(0).WithLocalizedMessage(LK.WebSiteConfiguration.InternalPortRequired)
            .InclusiveBetween(WebSiteConstants.MinWebApplicationPort, WebSiteConstants.MaxWebApplicationPort)
            .WithLocalizedMessage(LK.WebSiteConfiguration.InternalPortRange);

        RuleFor(x => x.Environment)
            .NotEmpty().WithLocalizedMessage(LK.WebSiteConfiguration.EnvironmentRequired);

        RuleFor(x => x.ProcessTimeoutSeconds)
            .InclusiveBetween(WebSiteConstants.MinProcessTimeoutSeconds, WebSiteConstants.MaxProcessTimeoutSeconds)
            .WithLocalizedMessage(LK.WebSiteConfiguration.ProcessTimeoutRange);

        RuleFor(x => x.HostName)
            .NotEmpty().WithLocalizedMessage(LK.WebSiteConfiguration.HostNameRequired);

        RuleFor(x => x.PublicPort)
            .GreaterThan(0).WithLocalizedMessage(LK.WebSiteConfiguration.PublicPortRequired)
            .Must(port => WebSiteConstants.WellKnownWebPorts.Contains(port) || port is >= WebSiteConstants.MinWebApplicationPort and <= WebSiteConstants.MaxWebApplicationPort)
            .WithLocalizedMessage(LK.WebSiteConfiguration.PublicPortRange);
    }
}
