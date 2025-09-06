using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace FSH.Starter.Blazor.Client.Pages.Catalog;

public partial class Products
{
    [Inject]
    protected IApiClient _client { get; set; } = default!;

    protected EntityServerTableContext<ProductResponse, Guid, ProductViewModel> Context { get; set; } = default!;

    private EntityTable<ProductResponse, Guid, ProductViewModel> _table = default!;

    private List<BrandResponse> _brands = new();

    protected override async Task OnInitializedAsync()
    {
        Context = new(
            entityName: "Product",
            entityNamePlural: "Products",
            entityResource: FshResources.Products,
            fields: new()
            {
                new(prod => prod.Id,"Id", "Id"),
                new(prod => prod.Name,"Name", "Name"),
                new(prod => prod.Description, "Description", "Description"),
                new(prod => prod.Price, "Price", "Price"),
                new(prod => prod.Brand?.Name, "Brand", "Brand")
            },
            enableAdvancedSearch: true,
            idFunc: prod => prod.Id!.Value,
            searchFunc: async filter =>
            {
                var productFilter = filter.Adapt<SearchProductsCommand>();
                productFilter.MinimumRate = Convert.ToDouble(SearchMinimumRate);
                productFilter.MaximumRate = Convert.ToDouble(SearchMaximumRate);
                productFilter.BrandId = SearchBrandId;
                var result = await _client.SearchProductsEndpointAsync("1", productFilter);
                return result.Adapt<PaginationResponse<ProductResponse>>();
            },
            createFunc: async prod =>
            {
                await _client.CreateProductEndpointAsync("1", prod.Adapt<CreateProductCommand>());
            },
            updateFunc: async (id, prod) =>
            {
                await _client.UpdateProductEndpointAsync("1", id, prod.Adapt<UpdateProductCommand>());
            },
            deleteFunc: async id => await _client.DeleteProductEndpointAsync("1", id));

        await LoadBrandsAsync();
    }

    private async Task LoadBrandsAsync()
    {
        if (_brands.Count == 0)
        {
            var response = await _client.SearchBrandsEndpointAsync("1", new SearchBrandsCommand());
            if (response?.Items != null)
            {
                _brands = response.Items.ToList();
            }
        }
    }

    // Advanced Search

    private Guid? _searchBrandId;
    private Guid? SearchBrandId
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
