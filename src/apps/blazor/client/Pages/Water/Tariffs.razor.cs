using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace FSH.Starter.Blazor.Client.Pages.Water;

public partial class Tariffs
{
    [Inject]
    protected IApiClient _client { get; set; } = default!;

    protected EntityServerTableContext<TariffResponse, Guid, TariffViewModel> Context { get; set; } = default!;

    private EntityTable<TariffResponse, Guid, TariffViewModel> _table = default!;

    protected override void OnInitialized() =>
        Context = new(
            entityName: "Tariff",
            entityNamePlural: "Tariffs",
            entityResource: FshResources.Tariffs,
            fields: new()
            {
                new(tariff => tariff.Id, "Id", "Id"),
                new(tariff => tariff.Name, "Name", "Name"),
                new(tariff => tariff.Description, "Description", "Description"),
                new(tariff => tariff.EffectiveDate, "Effective Date", "EffectiveDate"),
                new(tariff => tariff.EndDate, "End Date", "EndDate"),
                new(tariff => tariff.RatePerUnit, "Rate Per Unit", "RatePerUnit"),
                new(tariff => tariff.FixedCharge, "Fixed Charge", "FixedCharge"),
                new(tariff => tariff.IsActive, "Is Active", "IsActive")
            },
            enableAdvancedSearch: true,
            idFunc: tariff => tariff.Id!.Value,
            searchFunc: async filter =>
            {
                var tariffFilter = filter.Adapt<SearchTariffsCommand>();
                var result = await _client.SearchTariffsEndpointAsync("1", tariffFilter);
                return result.Adapt<PaginationResponse<TariffResponse>>();
            },
            createFunc: async tariff =>
            {
                await _client.CreateTariffEndpointAsync("1", tariff.Adapt<CreateTariffCommand>());
            },
            updateFunc: async (id, tariff) =>
            {
                await _client.UpdateTariffEndpointAsync("1", id, tariff.Adapt<UpdateTariffCommand>());
            },
            deleteFunc: async id => await _client.DeleteTariffEndpointAsync("1", id));
}

public class TariffViewModel : UpdateTariffCommand
{
}
