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

    protected EntityServerTableContext<DimensionDto, Guid, DimensionViewModel> Context { get; set; } = default!;

    private EntityTable<DimensionDto, Guid, DimensionViewModel> _table = default!;

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
            exportFunc: async filter =>
            {
                var dataFilter = filter.Adapt<ExportDimensionsRequest>();
                dataFilter.Type = SearchTypeString;
                
                return await ApiClient.ExportDimensionsEndpointAsync("1", dataFilter);
            },
            importFunc: async (fileUploadModel, isUpdate) => await ApiClient.ImportDimensionsEndpointAsync("1", isUpdate, fileUploadModel),
            searchFunc: async filter =>
            {
                var dataFilter = filter.Adapt<SearchDimensionsRequest>();
                dataFilter.Type = SearchTypeString;
                
                var result = await ApiClient.SearchDimensionsEndpointAsync("1", dataFilter);
                
                return result.Adapt<PaginationResponse<DimensionDto>>();
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
