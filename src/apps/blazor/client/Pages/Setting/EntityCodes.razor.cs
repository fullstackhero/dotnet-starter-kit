using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Blazor.Shared;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace FSH.Starter.Blazor.Client.Pages.Setting;

public partial class EntityCodes : ComponentBase
{
    [Inject]
    protected IApiClient ApiClient { get; set; } = default!;

    protected EntityServerTableContext<GetEntityCodeResponse, Guid, EntityCodeViewModel> Context { get; set; } = default!;

    private EntityTable<GetEntityCodeResponse, Guid, EntityCodeViewModel> _table = default!;

    protected override void OnInitialized() =>
        Context = new(
            entityName: "EntityCodes",
            entityNamePlural: "EntityCodes",
            entityResource: FshResources.EntityCodes,
            fields: new()
            {
                // new(item => item.Id,"Id", "Id"),
                new(item => item.Order, "Order", "Order"),
                new(item => item.Code, "Code", "Code"),
                new(item => item.Name, "Name", "Name"),
                new(item => item.Type, "Type", "Type"),
                
                new(item => item.Separator, "Separator", "Separator"),
                new(item => item.Value, "Value", "Value" ),
                new(item => item.Description, "Description", "Description"),
                new(item => item.IsActive,  "Active", Type: typeof(bool)),
            },
            enableAdvancedSearch: false,
            idFunc: item => item.Id,
            searchFunc: async filter =>
            {
                var searchFilter = filter.Adapt<PaginationFilter>();

                var result = await ApiClient.GetEntityCodeListEndpointAsync("1", searchFilter);
                return result.Adapt<PaginationResponse<GetEntityCodeResponse>>();
            },
            createFunc: async item =>
            {
                await ApiClient.CreateEntityCodeEndpointAsync("1", item.Adapt<CreateEntityCodeCommand>());
            },
            updateFunc: async (id, item) =>
            {
                await ApiClient.UpdateEntityCodeEndpointAsync("1", id, item.Adapt<UpdateEntityCodeCommand>());
            },
            deleteFunc: async id => await ApiClient.DeleteEntityCodeEndpointAsync("1", id));
    
  
    #region Advanced Search

    private CodeType _searchCodeType = CodeType._0;
    private CodeType SearchCodeType
    {
        get => _searchCodeType;
        set
        {
            _searchCodeType = value;
            _ = _table?.ReloadDataAsync();
        }
    }
    #endregion
}

public class EntityCodeViewModel : UpdateEntityCodeCommand
{
}
