using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using FluentValidation;

namespace Askyl.Dsm.WebHosting.Globalization.Validators;

public sealed class LoginCredentialsValidator : AbstractValidator<LoginCredentials>
{
    public LoginCredentialsValidator(ILocalizer localizer)
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage(localizer[L.LoginCredentials.LoginRequired].Value);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(localizer[L.LoginCredentials.PasswordRequired].Value);
    }
}
