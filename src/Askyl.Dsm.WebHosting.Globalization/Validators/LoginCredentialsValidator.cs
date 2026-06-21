using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using FluentValidation;

namespace Askyl.Dsm.WebHosting.Globalization.Validators;

public sealed class LoginCredentialsValidator : AbstractValidator<LoginCredentials>
{
    public LoginCredentialsValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithLocalizedMessage(LK.LoginCredentials.LoginRequired);

        RuleFor(x => x.Password)
            .NotEmpty().WithLocalizedMessage(LK.LoginCredentials.PasswordRequired);
    }
}
