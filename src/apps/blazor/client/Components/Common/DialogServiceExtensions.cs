using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Starter.Blazor.Client.Components.Common;

public static class DialogServiceExtensions
{
    public static Task<DialogResult> ShowModalAsync<TDialog>(this IDialogService dialogService, DialogParameters parameters)
        where TDialog : ComponentBase =>
        dialogService.ShowModal<TDialog>(parameters).Result!;

    public static IDialogReference ShowModal<TDialog>(this IDialogService dialogService, DialogParameters parameters)
        where TDialog : ComponentBase
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true, BackdropClick = false };

        return dialogService.Show<TDialog>(string.Empty, parameters, options);
    }
}