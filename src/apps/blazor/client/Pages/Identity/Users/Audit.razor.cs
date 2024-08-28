using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Blazor.Shared;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace FSH.Starter.Blazor.Client.Pages.Identity.Users;

public partial class Audit
{
    [Inject]
    private IApiClient ApiClient { get; set; } = default!;
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Parameter]
    public Guid Id { get; set; }

    protected EntityClientTableContext<RelatedAuditTrail, Guid, object> Context { get; set; } = default!;

    private string? _searchString;
    private string? _subHeader;
    private MudDateRangePicker _dateRangePicker = default!;
    private DateRange? _dateRange;
    private bool _searchInOldValues;
    private bool _searchInNewValues;
    private List<RelatedAuditTrail> _trails = new();

    // Configure Automapper
    static Audit() =>
        TypeAdapterConfig<AuditTrail, RelatedAuditTrail>.NewConfig().Map(
            dest => dest.UTCTime,
            src => DateTime.SpecifyKind(src.DateTime, DateTimeKind.Utc).ToLocalTime());



    protected override async Task OnInitializedAsync()
    {
        if (Id == Guid.Empty)
        {
            var state = await AuthState;
            if (state != null)
            {
                Id = new Guid(state.User.GetUserId()!);
            }
        }
        _subHeader = $"Audit Trail for User {Id}";
        Context = new(
            entityNamePlural: "Trails",
            searchAction: true.ToString(),
            fields: new()
            {
                new(audit => audit.Id,"Id"),
                new(audit => audit.Entity, "Entity"),
                new(audit => audit.DateTime, "Date", Template: DateFieldTemplate),
                new(audit => audit.Operation, "Operation")
            },
            loadDataFunc: async () => _trails = (await ApiClient.GetUserAuditTrailEndpointAsync(Id)).Adapt<List<RelatedAuditTrail>>(),
            searchFunc: (searchString, trail) =>
                (string.IsNullOrWhiteSpace(searchString) // check Search String
                    || trail.Entity?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true
                    || (_searchInOldValues &&
                        trail.PreviousValues?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true)
                    || (_searchInNewValues &&
                        trail.NewValues?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true))
                && ((_dateRange?.Start is null && _dateRange?.End is null) // check Date Range
                    || (_dateRange?.Start is not null && _dateRange.End is null && trail.DateTime >= _dateRange.Start)
                    || (_dateRange?.Start is null && _dateRange?.End is not null && trail.DateTime <= _dateRange.End + new TimeSpan(0, 11, 59, 59, 999))
                    || (trail.DateTime >= _dateRange!.Start && trail.DateTime <= _dateRange.End + new TimeSpan(0, 11, 59, 59, 999))),
            hasExtraActionsFunc: () => true);
    }

    private void ShowBtnPress(Guid id)
    {
        var trail = _trails.First(f => f.Id == id);
        trail.ShowDetails = !trail.ShowDetails;
        foreach (var otherTrail in _trails.Except(new[] { trail }))
        {
            otherTrail.ShowDetails = false;
        }
    }

    public class RelatedAuditTrail : AuditTrail
    {
        public bool ShowDetails { get; set; }
        public DateTime UTCTime { get; set; }
    }
}
