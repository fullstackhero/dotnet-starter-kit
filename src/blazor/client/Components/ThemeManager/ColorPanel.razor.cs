using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Starter.Blazor.Client.Components.ThemeManager;

public partial class ColorPanel
{
    [Parameter]
    public List<string> Colors { get; set; } = new();

    [Parameter]
    public string ColorType { get; set; } = string.Empty;

    [Parameter]
    public Color CurrentColor { get; set; }

    [Parameter]
    public EventCallback<string> OnColorClicked { get; set; }

    protected async Task ColorClicked(string color)
    {
        await OnColorClicked.InvokeAsync(color);
    }
}
