using FSH.Starter.Blazor.Client.Components;
using FSH.Starter.Blazor.Infrastructure.Api;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Starter.Blazor.Client.Pages.Setting;

public class DimensionAutocomplete : MudAutocomplete<Guid>
{
    // [Inject]
    // private IStringLocalizer<DimensionAutocomplete> L { get; set; } = default!

    [Inject] private ISnackbar Toast { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] protected IApiClient ApiClient { get; set; } = default!;

    private List<DimensionDto> _itemList = [];

    // supply default parameters, but leave the possibility to override them
    [Parameter]
    public string? DimensionType { get; set; }
    public override Task SetParametersAsync(ParameterView parameters)
    {
        Label = "Father Dimension";
        Variant = Variant.Filled;
        Dense = true;
        Margin = Margin.None;
        ResetValueOnEmptyText = true;
        SearchFunc = SearchItems;
        ToStringFunc = GetItemName;
        Clearable = true;
        return base.SetParametersAsync(parameters);
    }

    // when the value parameter is set, we have to load that one Dimension to be able to show the name
    // we can't do that in OnInitialized because of a strange bug (https://github.com/MudBlazor/MudBlazor/issues/3818)
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender 
            && _value != Guid.Empty
            && await ApiHelper.ExecuteCallGuardedAsync(
                    () => ApiClient.GetDimensionEndpointAsync("1", _value), Toast, Navigation) 
                is { }  itemDto)
        {
            var item = new DimensionDto
            {
                Id = itemDto.Id,
                Code = itemDto.Code,
                Name = itemDto.Name
            };
            _itemList.Add(item);
            ForceRender(true);
        }
    }

    private async Task<IEnumerable<Guid>> SearchItems(string searchString, CancellationToken cancellationToken)
    {
        var dataFilter = new SearchDimensionsRequest
        {
            Type = DimensionType,
            PageSize = 10,
            AdvancedSearch = new() { Fields = new[] { "Code", "Name" }, Keyword = searchString }
        };
        
        if (await ApiHelper.ExecuteCallGuardedAsync(
                () => ApiClient.SearchDimensionsEndpointAsync("1", dataFilter, cancellationToken), Toast, Navigation) 
                    is { }  response)
        {
            _itemList = response.Items!.ToList();
        }

        return _itemList.Select(x => x.Id);
    }

    private string GetItemName(Guid id) => _itemList.Find(e => e.Id == id)?.Name ?? string.Empty;
}
