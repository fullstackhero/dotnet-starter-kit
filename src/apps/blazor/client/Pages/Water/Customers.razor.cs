using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace FSH.Starter.Blazor.Client.Pages.Water;

public partial class Customers
{
    [Inject]
    protected IApiClient _client { get; set; } = default!;

    protected EntityServerTableContext<CustomerResponse, Guid, CustomerViewModel> Context { get; set; } = default!;

    private EntityTable<CustomerResponse, Guid, CustomerViewModel> _table = default!;

    protected override void OnInitialized() =>
        Context = new(
            entityName: "Customer",
            entityNamePlural: "Customers",
            entityResource: FshResources.Customers,
            fields: new()
            {
                new(cust => cust.Id, "Id", "Id"),
                new(cust => cust.CustomerCode, "Customer Code", "CustomerCode"),
                new(cust => cust.FullName, "Full Name", "FullName"),
                new(cust => cust.Address, "Address", "Address"),
                new(cust => cust.ContactNumber, "Contact Number", "ContactNumber"),
                new(cust => cust.Email, "Email", "Email"),
                new(cust => cust.ConnectionType, "Connection Type", "ConnectionType"),
                new(cust => cust.Status, "Status", "Status")
            },
            enableAdvancedSearch: true,
            idFunc: cust => cust.Id!.Value,
            searchFunc: async filter =>
            {
                var customerFilter = filter.Adapt<SearchCustomersCommand>();
                var result = await _client.SearchCustomersEndpointAsync("1", customerFilter);
                return result.Adapt<PaginationResponse<CustomerResponse>>();
            },
            createFunc: async cust =>
            {
                await _client.CreateCustomerEndpointAsync("1", cust.Adapt<CreateCustomerCommand>());
            },
            updateFunc: async (id, cust) =>
            {
                await _client.UpdateCustomerEndpointAsync("1", id, cust.Adapt<UpdateCustomerCommand>());
            },
            deleteFunc: async id => await _client.DeleteCustomerEndpointAsync("1", id));
}

public class CustomerViewModel : UpdateCustomerCommand
{
    public string? CustomerCode { get; set; }
}
