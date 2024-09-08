using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Blazor.Shared;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace FSH.Starter.Blazor.Client.Pages.Setting;

public partial class Dimensions : ComponentBase
{
    [Inject]
    protected IApiClient ApiClient { get; set; } = default!;

    protected EntityServerTableContext<GetDimensionResponse, Guid, DimensionViewModel> Context { get; set; } = default!;

    private EntityTable<GetDimensionResponse, Guid, DimensionViewModel> _table = default!;

    protected override void OnInitialized() =>
        Context = new(
            entityName: "Dimensions",
            entityNamePlural: "Dimensions",
            entityResource: FshResources.Dimensions,
            fields: new()
            {
                // new(item => item.Id,"Id", "Id"),
                new(item => item.Order, "Order", "Order"),
                new(item => item.Code, "Code", "Code"),
                new(item => item.Name, "Name", "Name"),
                new(item => item.Value, "Value", "Value" ),
                new(item => item.Type, "Type", "Type" ),
                // new(item => item.FatherName, "Father item", "FatherName"),
                new(item => item.FullName, "Full Name", "FullName"),
                new(item => item.NativeName, "Native Name", "NativeName"),

                // new(item => item.FullNativeName, "Full Native", "FullNativeName"),
                // new(item => item.Description, "Description", "Description"),
                new(item => item.IsActive,  "Active", Type: typeof(bool)),
            },
            enableAdvancedSearch: false,
            idFunc: item => item.Id,
            searchFunc: async filter =>
            {
                var searchFilter = filter.Adapt<PaginationFilter>();

                var result = await ApiClient.GetDimensionListEndpointAsync("1", searchFilter);
                return result.Adapt<PaginationResponse<GetDimensionResponse>>();
            },
            createFunc: async item =>
            {
                await ApiClient.CreateDimensionEndpointAsync("1", item.Adapt<CreateDimensionCommand>());
            },
            updateFunc: async (id, item) =>
            {
                await ApiClient.UpdateDimensionEndpointAsync("1", id, item.Adapt<UpdateDimensionCommand>());
            },
            deleteFunc: async id => await ApiClient.DeleteDimensionEndpointAsync("1", id));
    
    
    #region Advanced Search
    
    private string? SearchTypeString { get; set; }
    private void OnTypeStringChanged()
    {
        _ = _table?.ReloadDataAsync();
    }
    #endregion
}

public class DimensionViewModel : UpdateDimensionCommand
{
}


