using FSH.Framework.Shared.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace FSH.Framework.Blazor.UI.Components.Base;

public abstract class FshComponentBase : ComponentBase
{
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IDialogService DialogService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected IStringLocalizer<SharedResource> L { get; set; } = default!;
}