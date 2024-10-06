using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace FSH.Starter.Blazor.Client.Pages.Setting;

public class EntityCodeAutocomplete : MudAutocomplete<Guid>
{
    [Inject]
    private IStringLocalizer<EntityCodeAutocomplete> L { get; set; } = default!;
    
    [Inject]
    private ISnackbar Toast { get; set; } = default!;
    
    [Inject]
    protected IApiClient ApiClient { get; set; } = default!;

    private List<EntityCodeDto> _entityList = [];

    // supply default parameters, but leave the possibility to override them
    [Parameter]
    public string? EntityCodeType { get; set; }
    public override Task SetParametersAsync(ParameterView parameters)
    {
        Label = L["EntityCode"];
        Variant = Variant.Filled;
        Dense = true;
        Margin = Margin.None;
        ResetValueOnEmptyText = true;
        SearchFunc = SearchEntities;
        ToStringFunc = GetEntityName;
        Clearable = true;
        return base.SetParametersAsync(parameters);
    }
    
    // when the value parameter is set, we have to load that one EntityCode to be able to show the name
    // we can't do that in OnInitialized because of a strange bug (https://github.com/MudBlazor/MudBlazor/issues/3818)
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _value != Guid.Empty)
        {
            var result = await ApiClient.GetEntityCodeEndpointAsync("1", _value);
            var entity = new EntityCodeDto
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
        var dataFilter = new SearchEntityCodesRequest
        {
            PageSize = 10,
            AdvancedSearch = new() { Fields = new[] { "Code", "Name" }, Keyword = value }
        };
        
        var result = await ApiClient.SearchEntityCodesEndpointAsync("1", dataFilter, cancellationToken);
        var paginationResponse = result.Adapt<PaginationResponse<EntityCodeDto>>();
        _entityList = paginationResponse.Items;

        return _entityList.Select(x => x.Id);
    }

    private string GetEntityName(Guid id)
    {
        var entity = _entityList.Find(e => e.Id == id);
        return (entity != null ? entity.Name : string.Empty)!;
    }
}
