using FSH.Starter.Blazor.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
namespace FSH.Starter.Blazor.Client.Components;

public partial class PersonCard
{
    [Parameter]
    public string? Class { get; set; }
    [Parameter]
    public string? Style { get; set; }

    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;

    private string? UserId { get; set; }
    private string? Email { get; set; }
    private string? FullName { get; set; }
    private string? ImageUri { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadUserData();
        }
    }

    private async Task LoadUserData()
    {
        var user = (await AuthState).User;
        if (user.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(UserId))
        {
            FullName = user.GetFullName();
            UserId = user.GetUserId();
            Email = user.GetEmail();
            if (user.GetImageUrl() != null)
            {
                ImageUri = user.GetImageUrl()!.ToString();
            }
            StateHasChanged();
        }
    }
}
