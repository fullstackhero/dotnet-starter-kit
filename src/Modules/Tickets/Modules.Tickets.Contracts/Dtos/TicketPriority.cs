using System.Text.Json.Serialization;

namespace FSH.Modules.Tickets.Contracts.Dtos;

[JsonConverter(typeof(JsonStringEnumConverter<TicketPriority>))]
public enum TicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3,
}
