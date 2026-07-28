using Askyl.Dsm.WebHosting.Constants.Application;
using FluentValidation.Results;

namespace Askyl.Dsm.WebHosting.Globalization.Validators;

/// <summary>
/// Extension methods for turning FluentValidation results into user-facing messages.
/// </summary>
public static class ValidationResultExtensions
{
    extension(ValidationResult result)
    {
        /// <summary>
        /// Joins every failure into a single message. Each message is already localized, because
        /// <see cref="DeferredMessageExtensions"/> resolves resource keys at validation time.
        /// </summary>
        public string ToMessage()
            => String.Join(ValidationConstants.MessageSeparator, result.Errors.Select(failure => failure.ErrorMessage));
    }
}
