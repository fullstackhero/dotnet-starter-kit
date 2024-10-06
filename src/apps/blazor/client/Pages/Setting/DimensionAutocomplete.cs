using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace FSH.Starter.Blazor.Client.Pages.Setting;

public class DimensionAutocomplete : MudAutocomplete<Guid>
{
    [Inject]
    private IStringLocalizer<DimensionAutocomplete> L { get; set; } = default!;
    
    [Inject]
    private ISnackbar Toast { get; set; } = default!;
    
    [Inject]
    protected IApiClient ApiClient { get; set; } = default!;

    private List<DimensionDto> _entityList = [];

    // supply default parameters, but leave the possibility to override them
    [Parameter]
    public string? DimensionType { get; set; }
    public override Task SetParametersAsync(ParameterView parameters)
    {
        Label = L["Father Dimension"];
        Variant = Variant.Filled;
        Dense = true;
        Margin = Margin.None;
        ResetValueOnEmptyText = true;
        SearchFunc = SearchEntities;
        ToStringFunc = GetEntityName;
        Clearable = true;
        return base.SetParametersAsync(parameters);
    }
    
    // when the value parameter is set, we have to load that one Dimension to be able to show the name
    // we can't do that in OnInitialized because of a strange bug (https://github.com/MudBlazor/MudBlazor/issues/3818)
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _value != Guid.Empty)
        {
            var result = await ApiClient.GetDimensionEndpointAsync("1", _value);
            var entity = new DimensionDto
            {
                Id = result.Id,
                Code = result.Code,
                Name = result.Name
            };
            _entityList.Add(entity);
            ForceRender(true);
        }
    }

    private async Task<IEnumerable<Guid>> SearchEntities(string value, CancellationToken cancellationToken)
    {
        var dataFilter = new SearchDimensionsRequest
        {
            Type = DimensionType,
            PageSize = 10,
            AdvancedSearch = new() { Fields = new[] { "Code", "Name" }, Keyword = value }
        };
        
        var result = await ApiClient.SearchDimensionsEndpointAsync("1", dataFilter, cancellationToken);
        var paginationResponse = result.Adapt<PaginationResponse<DimensionDto>>();
        _entityList = paginationResponse.Items;

        return _entityList.Select(x => x.Id);
    }

    private string GetEntityName(Guid id)
    {
        var entity = _entityList.Find(e => e.Id == id);
        return (entity != null ? entity.Name : string.Empty)!;
    }
}
