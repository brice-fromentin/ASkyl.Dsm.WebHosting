using System.Net;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Askyl.Dsm.WebHosting.Ui.Client.Extensions;

/// <summary>
/// Message boxes for plain text, which is all this application ever shows in them.
/// </summary>
/// <remarks>
/// FluentUI 5 renders a message box's text as sanitized HTML and throws on any angle bracket outside an allowed
/// tag, so System.Text.Json's "'&lt;' is an invalid start of a value" made the error handler itself throw.
/// Encoding first leaves no bracket to reject, and the markup renders the original text.
/// </remarks>
public static class DialogServiceExtensions
{
    extension(IDialogService dialogService)
    {
        /// <summary>
        /// Shows an error message box whose text is displayed as written, never interpreted as HTML.
        /// </summary>
        public Task<DialogResult> ShowErrorTextAsync(string message) => dialogService.ShowErrorAsync(WebUtility.HtmlEncode(message));

        /// <summary>
        /// Shows an information message box whose text is displayed as written, never interpreted as HTML.
        /// </summary>
        public Task<DialogResult> ShowInfoTextAsync(string message) => dialogService.ShowInfoAsync(WebUtility.HtmlEncode(message));

        /// <summary>
        /// Shows a confirmation message box whose text is displayed as written, never interpreted as HTML.
        /// The button labels are required: FluentUI's defaults are an English "Yes" and "No", which sat under a
        /// localized text asking the user to click OK.
        /// </summary>
        public Task<DialogResult> ShowConfirmationTextAsync(string message, string confirmButton, string cancelButton)
            => dialogService.ShowConfirmationAsync(WebUtility.HtmlEncode(message), primaryButton: confirmButton, secondaryButton: cancelButton);
    }
}
