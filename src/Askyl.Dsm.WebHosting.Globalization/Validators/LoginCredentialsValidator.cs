using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Globalization.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Askyl.Dsm.WebHosting.Globalization.Validators;

public sealed class LoginCredentialsValidator : AbstractValidator<LoginCredentials>
{
    public LoginCredentialsValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage(localizer[L.LoginCredentials.LoginRequired].Value);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(localizer[L.LoginCredentials.PasswordRequired].Value);
    }
}
