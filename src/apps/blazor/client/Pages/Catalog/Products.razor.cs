using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Blazor.Shared;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace FSH.Starter.Blazor.Client.Pages.Catalog;

public partial class Products
{
    [Inject]
    protected IApiClient ApiClient { get; set; } = default!;

    protected EntityServerTableContext<ProductDto, Guid, ProductViewModel> Context { get; set; } = default!;

    private EntityTable<ProductDto, Guid, ProductViewModel> _table = default!;

    protected override void OnInitialized() =>
        Context = new(
            entityName: "Product",
            entityNamePlural: "Products",
            entityResource: FshResources.Products,
            fields: new()
            {
                new(prod => prod.Id,"Id", "Id"),
                new(prod => prod.Name,"Name", "Name"),
                new(prod => prod.Description, "Description", "Description"),
                new(prod => prod.Price, "Price", "Price")
            },
            enableAdvancedSearch: true,
            idFunc: prod => prod.Id!.Value,
            exportFunc: async filter =>
            {
                var dataFilter = filter.Adapt<ExportProductsRequest>();
                dataFilter.MinimumRate = Convert.ToDouble(SearchMinimumRate);
                dataFilter.MaximumRate = Convert.ToDouble(SearchMaximumRate);
                
                return await ApiClient.ExportProductsEndpointAsync("1", dataFilter);

            },
            importFunc: async (fileUploadModel, isUpdate) => await ApiClient.ImportProductsEndpointAsync("1", isUpdate, fileUploadModel),
            searchFunc: async filter =>
            {
                var dataFilter = filter.Adapt<SearchProductsRequest>();
                dataFilter.MinimumRate = Convert.ToDouble(SearchMinimumRate);
                dataFilter.MaximumRate = Convert.ToDouble(SearchMaximumRate);
                var result = await ApiClient.SearchProductsEndpointAsync("1", dataFilter);
                return result.Adapt<PaginationResponse<ProductDto>>();
            },
            createFunc: async prod =>
            {
                await ApiClient.CreateProductEndpointAsync("1", prod.Adapt<CreateProductCommand>());
            },
            updateFunc: async (id, prod) =>
            {
                await ApiClient.UpdateProductEndpointAsync("1", id, prod.Adapt<UpdateProductCommand>());
            },
            deleteFunc: async id => await ApiClient.DeleteProductEndpointAsync("1", id));

    // Advanced Search

    private Guid _searchBrandId;
    private Guid SearchBrandId
    {
        get => _searchBrandId;
        set
        {
            _searchBrandId = value;
            _ = _table.ReloadDataAsync();
        }
    }

    private decimal _searchMinimumRate;
    private decimal SearchMinimumRate
    {
        get => _searchMinimumRate;
        set
        {
            _searchMinimumRate = value;
            _ = _table.ReloadDataAsync();
        }
    }

    private decimal _searchMaximumRate = 9999;
    private decimal SearchMaximumRate
    {
        get => _searchMaximumRate;
        set
        {
            _searchMaximumRate = value;
            _ = _table.ReloadDataAsync();
        }
    }
}

public class ProductViewModel : UpdateProductCommand
{
}
