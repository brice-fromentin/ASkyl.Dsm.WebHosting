using Microsoft.FluentUI.AspNetCore.Components;

namespace Askyl.Dsm.WebHosting.Ui.Extensions;

/// <summary>
/// Extension methods for IDialogService to simplify dialog creation
/// </summary>
public static class IDialogServiceExtensions
{
    public static async Task<IDialogReference> ShowDialogAsync<TDialog>(this IDialogService dialogService, string? width = null)
        where TDialog : IDialogContentComponent
    {
        return await dialogService.ShowDialogAsync<TDialog>(CreateParameters(width));
    }

    public static async Task<IDialogReference> ShowDialogAsync<TDialog>(this IDialogService dialogService, object content, string? width = null)
        where TDialog : IDialogContentComponent
    {
        return await dialogService.ShowDialogAsync<TDialog>(content, CreateParameters(width));
    }

    private static DialogParameters CreateParameters(string? width)
    {
        return new DialogParameters
        {
            Width = width ?? "",
            Modal = true,
            TrapFocus = false,
            PreventScroll = true,
            PreventDismissOnOverlayClick = true
        };
    }
}
