using FSH.Framework.Shared.Localization;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace FSH.Framework.Blazor.UI.Components.Dialogs;

public static class FshDialogService
{
    public static async Task<bool> ShowConfirmAsync(
        this IDialogService dialogService,
        string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        Color confirmColor = Color.Primary,
        string icon = Icons.Material.Outlined.Help,
        Color iconColor = Color.Primary)
    {
        ArgumentNullException.ThrowIfNull(dialogService);

        var parameters = new DialogParameters<FshConfirmDialog>
        {
            { x => x.Title, title },
            { x => x.Message, message },
            { x => x.ConfirmText, confirmText },
            { x => x.CancelText, cancelText },
            { x => x.ConfirmColor, confirmColor },
            { x => x.Icon, icon },
            { x => x.IconColor, iconColor }
        };

        var options = new DialogOptions
        {
            CloseButton = false,
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
            BackdropClick = false,
            CloseOnEscapeKey = true
        };

        var dialog = await dialogService.ShowAsync<FshConfirmDialog>(title, parameters, options);
        var result = await dialog.Result;

        return result is not null && !result.Canceled;
    }

    public static Task<bool> ShowDeleteConfirmAsync(
        this IDialogService dialogService,
        string itemName,
        IStringLocalizer<SharedResource>? localizer = null)
    {
        string title = localizer?["DeleteConfirmation"] ?? "Delete Confirmation";
        string message = localizer != null
            ? string.Format(CultureInfo.InvariantCulture, localizer["ConfirmDelete"], itemName)
            : $"Are you sure you want to delete {itemName}? This action cannot be undone.";
        string confirmText = localizer?["Delete"] ?? "Delete";
        string cancelText = localizer?["Cancel"] ?? "Cancel";

        return dialogService.ShowConfirmAsync(
            title: title,
            message: message,
            confirmText: confirmText,
            cancelText: cancelText,
            confirmColor: Color.Error,
            icon: Icons.Material.Outlined.DeleteForever,
            iconColor: Color.Error);
    }

    public static Task<bool> ShowSignOutConfirmAsync(
        this IDialogService dialogService,
        IStringLocalizer<SharedResource>? localizer = null)
    {
        string title = localizer?["SignOut"] ?? "Sign Out";
        string message = localizer?["ConfirmSignOut"] ?? "Are you sure you want to sign out of your account?";
        string confirmText = localizer?["SignOut"] ?? "Sign Out";
        string cancelText = localizer?["Cancel"] ?? "Cancel";

        return dialogService.ShowConfirmAsync(
            title: title,
            message: message,
            confirmText: confirmText,
            cancelText: cancelText,
            confirmColor: Color.Error,
            icon: Icons.Material.Outlined.Logout,
            iconColor: Color.Warning);
    }
}