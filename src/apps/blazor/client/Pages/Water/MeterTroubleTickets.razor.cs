using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace FSH.Starter.Blazor.Client.Pages.Water;

public partial class MeterTroubleTickets
{
    [Inject]
    protected IApiClient _client { get; set; } = default!;

    protected EntityServerTableContext<MeterTroubleTicketResponse, Guid, MeterTroubleTicketViewModel> Context { get; set; } = default!;

    private EntityTable<MeterTroubleTicketResponse, Guid, MeterTroubleTicketViewModel> _table = default!;

    private List<MeterResponse> _meters = new();

    protected override async Task OnInitializedAsync()
    {
        Context = new(
            entityName: "Trouble Ticket",
            entityNamePlural: "Trouble Tickets",
            entityResource: FshResources.MeterTroubleTickets,
            fields: new()
            {
                new(ticket => ticket.Id, "Id", "Id"),
                new(ticket => ticket.MeterId, "Meter Id", "MeterId"),
                new(ticket => ticket.IssueDescription, "Issue Description", "IssueDescription"),
                new(ticket => ticket.ReportedDate, "Reported Date", "ReportedDate"),
                new(ticket => ticket.Status, "Status", "Status"),
                new(ticket => ticket.ResolvedDate, "Resolved Date", "ResolvedDate"),
                new(ticket => ticket.ResolutionNotes, "Resolution Notes", "ResolutionNotes")
            },
            enableAdvancedSearch: true,
            idFunc: ticket => ticket.Id!.Value,
            searchFunc: async filter =>
            {
                var ticketFilter = filter.Adapt<SearchMeterTroubleTicketsCommand>();
                var result = await _client.SearchMeterTroubleTicketsEndpointAsync("1", ticketFilter);
                return result.Adapt<PaginationResponse<MeterTroubleTicketResponse>>();
            },
            createFunc: async ticket =>
            {
                await _client.CreateMeterTroubleTicketEndpointAsync("1", ticket.Adapt<CreateMeterTroubleTicketCommand>());
            },
            updateFunc: async (id, ticket) =>
            {
                await _client.UpdateMeterTroubleTicketEndpointAsync("1", id, ticket.Adapt<UpdateMeterTroubleTicketCommand>());
            },
            deleteFunc: async id => await _client.DeleteMeterTroubleTicketEndpointAsync("1", id));

        await LoadMetersAsync();
    }

    private async Task LoadMetersAsync()
    {
        if (_meters.Count == 0)
        {
            var response = await _client.SearchMetersEndpointAsync("1", new SearchMetersCommand());
            if (response?.Items != null)
            {
                _meters = response.Items.ToList();
            }
        }
    }
}

public class MeterTroubleTicketViewModel : UpdateMeterTroubleTicketCommand
{
    public Guid MeterId { get; set; }
    public DateTime ReportedDate { get; set; }
}
