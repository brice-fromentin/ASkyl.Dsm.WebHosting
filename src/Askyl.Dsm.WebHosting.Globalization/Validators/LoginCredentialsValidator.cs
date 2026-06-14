using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using FluentValidation;

namespace Askyl.Dsm.WebHosting.Globalization.Validators;

public sealed class LoginCredentialsValidator : AbstractValidator<LoginCredentials>
{
    public LoginCredentialsValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithLocalizedMessage(L.LoginCredentials.LoginRequired);

        RuleFor(x => x.Password)
            .NotEmpty().WithLocalizedMessage(L.LoginCredentials.PasswordRequired);
    }
}
