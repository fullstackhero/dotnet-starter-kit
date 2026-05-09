using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace FSH.Starter.Blazor.Client.Pages.Water;

public partial class MeterReadings
{
    [Inject]
    protected IApiClient _client { get; set; } = default!;

    protected EntityServerTableContext<MeterReadingResponse, Guid, MeterReadingViewModel> Context { get; set; } = default!;

    private EntityTable<MeterReadingResponse, Guid, MeterReadingViewModel> _table = default!;

    protected override void OnInitialized() =>
        Context = new(
            entityName: "Meter Reading",
            entityNamePlural: "Meter Readings",
            entityResource: FshResources.MeterReadings,
            fields: new()
            {
                new(reading => reading.Id, "Id", "Id"),
                new(reading => reading.MeterId, "Meter Id", "MeterId"),
                new(reading => reading.ReadingDate, "Reading Date", "ReadingDate"),
                new(reading => reading.ReadingValue, "Reading Value", "ReadingValue"),
                new(reading => reading.PreviousReadingValue, "Previous Reading", "PreviousReadingValue"),
                new(reading => reading.Consumption, "Consumption", "Consumption"),
                new(reading => reading.Source, "Source", "Source"),
                new(reading => reading.Notes, "Notes", "Notes")
            },
            enableAdvancedSearch: true,
            idFunc: reading => reading.Id!.Value,
            searchFunc: async filter =>
            {
                var readingFilter = filter.Adapt<SearchMeterReadingsCommand>();
                var result = await _client.SearchMeterReadingsEndpointAsync("1", readingFilter);
                return result.Adapt<PaginationResponse<MeterReadingResponse>>();
            },
            createFunc: async reading =>
            {
                await _client.CreateMeterReadingEndpointAsync("1", reading.Adapt<CreateMeterReadingCommand>());
            });
}

public class MeterReadingViewModel : CreateMeterReadingCommand
{
}
