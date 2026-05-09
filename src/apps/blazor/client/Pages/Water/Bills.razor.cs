using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace FSH.Starter.Blazor.Client.Pages.Water;

public partial class Bills
{
    [Inject]
    protected IApiClient _client { get; set; } = default!;

    protected EntityServerTableContext<BillResponse, Guid, BillViewModel> Context { get; set; } = default!;

    private EntityTable<BillResponse, Guid, BillViewModel> _table = default!;

    private List<CustomerResponse> _customers = new();

    protected override async Task OnInitializedAsync()
    {
        Context = new(
            entityName: "Bill",
            entityNamePlural: "Bills",
            entityResource: FshResources.Bills,
            fields: new()
            {
                new(bill => bill.Id, "Id", "Id"),
                new(bill => bill.CustomerId, "Customer Id", "CustomerId"),
                new(bill => bill.TariffId, "Tariff Id", "TariffId"),
                new(bill => bill.BillingMonth, "Billing Month", "BillingMonth"),
                new(bill => bill.BillingYear, "Billing Year", "BillingYear"),
                new(bill => bill.TotalConsumption, "Total Consumption", "TotalConsumption"),
                new(bill => bill.TotalAmount, "Total Amount", "TotalAmount"),
                new(bill => bill.FixedCharge, "Fixed Charge", "FixedCharge"),
                new(bill => bill.VariableCharge, "Variable Charge", "VariableCharge"),
                new(bill => bill.DueDate, "Due Date", "DueDate"),
                new(bill => bill.PaidDate, "Paid Date", "PaidDate"),
                new(bill => bill.Status, "Status", "Status")
            },
            enableAdvancedSearch: true,
            idFunc: bill => bill.Id!.Value,
            searchFunc: async filter =>
            {
                var billFilter = filter.Adapt<SearchBillsCommand>();
                var result = await _client.SearchBillsEndpointAsync("1", billFilter);
                return result.Adapt<PaginationResponse<BillResponse>>();
            },
            createFunc: async bill =>
            {
                await _client.CreateBillEndpointAsync("1", bill.Adapt<CreateBillCommand>());
            });

        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        if (_customers.Count == 0)
        {
            var response = await _client.SearchCustomersEndpointAsync("1", new SearchCustomersCommand());
            if (response?.Items != null)
            {
                _customers = response.Items.ToList();
            }
        }
    }
}

public class BillViewModel : CreateBillCommand
{
}
