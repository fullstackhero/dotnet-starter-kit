using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Blazor.Client.Layout;

public partial class MainLayout
{
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;
    [Parameter]
    public EventCallback OnDarkModeToggle { get; set; }
    [Parameter]
    public EventCallback<bool> OnRightToLeftToggle { get; set; }

    private bool _drawerOpen;
    private bool _rightToLeft;

    protected override async Task OnInitializedAsync()
    {
        //if (await ClientPreferences.GetPreference() is ClientPreference preference)
        //{
        //    _rightToLeft = preference.IsRTL;
        //    _drawerOpen = preference.IsDrawerOpen;
        //}
    }

    private async Task RightToLeftToggle()
    {
        //bool isRtl = await ClientPreferences.ToggleLayoutDirectionAsync();
        //_rightToLeft = isRtl;

        //await OnRightToLeftToggle.InvokeAsync(isRtl);
    }

    public async Task ToggleDarkMode()
    {
        await OnDarkModeToggle.InvokeAsync();
    }

    private async Task DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;

    }
    private void Logout()
    {
        var parameters = new DialogParameters
        {
                { nameof(Components.Dialogs.Logout.ContentText), "Logout Confirmation"},
                { nameof(Components.Dialogs.Logout.ButtonText), "Logout"},
                { nameof(Components.Dialogs.Logout.Color), Color.Error}
            };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        DialogService.Show<Components.Dialogs.Logout>("Logout", parameters, options);
    }

    private void Profile()
    {
        Navigation.NavigateTo("/account");
    }
}
