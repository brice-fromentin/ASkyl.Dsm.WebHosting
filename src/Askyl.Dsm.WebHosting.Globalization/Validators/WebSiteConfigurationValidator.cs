using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using FluentValidation;

namespace Askyl.Dsm.WebHosting.Globalization.Validators;

public sealed class WebSiteConfigurationValidator : AbstractValidator<WebSiteConfiguration>
{
    public WebSiteConfigurationValidator(ILocalizer localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer[L.WebSiteConfiguration.NameRequired].Value)
            .Length(1, 100).WithMessage(localizer[L.WebSiteConfiguration.NameLength].Value);

        RuleFor(x => x.ApplicationPath)
            .NotEmpty().WithMessage(localizer[L.WebSiteConfiguration.ApplicationPathRequired].Value);

        RuleFor(x => x.InternalPort)
            .GreaterThan(0).WithMessage(localizer[L.WebSiteConfiguration.PortRequired].Value)
            .InclusiveBetween(WebSiteConstants.MinWebApplicationPort, WebSiteConstants.MaxWebApplicationPort)
            .WithMessage(localizer[L.WebSiteConfiguration.InternalPortRange].Value);

        RuleFor(x => x.Environment)
            .NotEmpty().WithMessage(localizer[L.WebSiteConfiguration.EnvironmentRequired].Value);

        RuleFor(x => x.ProcessTimeoutSeconds)
            .InclusiveBetween(WebSiteConstants.MinProcessTimeoutSeconds, WebSiteConstants.MaxProcessTimeoutSeconds)
            .WithMessage(localizer[L.WebSiteConfiguration.ProcessTimeoutRange].Value);

        RuleFor(x => x.HostName)
            .NotEmpty().WithMessage(localizer[L.WebSiteConfiguration.HostNameRequired].Value);

        RuleFor(x => x.PublicPort)
            .GreaterThan(0).WithMessage(localizer[L.WebSiteConfiguration.PortRequired].Value)
            .Must(port => WebSiteConstants.WellKnownWebPorts.Contains(port) || port is >= WebSiteConstants.MinWebApplicationPort and <= WebSiteConstants.MaxWebApplicationPort)
            .WithMessage(localizer[L.WebSiteConfiguration.PublicPortRange].Value);
    }
}
