using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace FSH.Starter.Blazor.Client.Pages.Water;

public partial class Meters
{
    [Inject]
    protected IApiClient _client { get; set; } = default!;

    protected EntityServerTableContext<MeterResponse, Guid, MeterViewModel> Context { get; set; } = default!;

    private EntityTable<MeterResponse, Guid, MeterViewModel> _table = default!;

    private List<CustomerResponse> _customers = new();

    protected override async Task OnInitializedAsync()
    {
        Context = new(
            entityName: "Meter",
            entityNamePlural: "Meters",
            entityResource: FshResources.Meters,
            fields: new()
            {
                new(meter => meter.Id, "Id", "Id"),
                new(meter => meter.MeterNumber, "Meter Number", "MeterNumber"),
                new(meter => meter.Model, "Model", "Model"),
                new(meter => meter.InstallationDate, "Installation Date", "InstallationDate"),
                new(meter => meter.Status, "Status", "Status"),
                new(meter => meter.CustomerId, "Customer", "CustomerId")
            },
            enableAdvancedSearch: true,
            idFunc: meter => meter.Id!.Value,
            searchFunc: async filter =>
            {
                var meterFilter = filter.Adapt<SearchMetersCommand>();
                var result = await _client.SearchMetersEndpointAsync("1", meterFilter);
                return result.Adapt<PaginationResponse<MeterResponse>>();
            },
            createFunc: async meter =>
            {
                await _client.CreateMeterEndpointAsync("1", meter.Adapt<CreateMeterCommand>());
            },
            updateFunc: async (id, meter) =>
            {
                await _client.UpdateMeterEndpointAsync("1", id, meter.Adapt<UpdateMeterCommand>());
            },
            deleteFunc: async id => await _client.DeleteMeterEndpointAsync("1", id));

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

public class MeterViewModel : UpdateMeterCommand
{
    public string? MeterNumber { get; set; }
    public DateTime? InstallationDate { get; set; }
    public Guid CustomerId { get; set; }
}
