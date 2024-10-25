using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.AcademicYear.Domain.Events;

namespace FSH.Starter.WebApi.AcademicYear.Domain;

public class AcademicYear : AuditableEntity, IAggregateRoot
{
    public string EthCalendar { get; private set; } = default!;
    public string GregorianCalendar { get; private set; } = default!;
    public string Status { get; private set; } = default!; // e.g., "Active", "Inactive"

    public static AcademicYear Create(string ethCalendar, string gregorianCalendar, string status)
    {
        var academicYear = new AcademicYear
        {
            EthCalendar = ethCalendar,
            GregorianCalendar = gregorianCalendar,
            Status = status
        };

        academicYear.QueueDomainEvent(new AcademicYearCreated() { AcademicYear = academicYear });

        return academicYear;
    }

    public AcademicYear Update(string? ethCalendar, string? gregorianCalendar, string? status)
    {
        if (ethCalendar is not null && EthCalendar?.Equals(ethCalendar, StringComparison.OrdinalIgnoreCase) is not true) EthCalendar = ethCalendar;
        if (gregorianCalendar is not null && GregorianCalendar?.Equals(gregorianCalendar, StringComparison.OrdinalIgnoreCase) is not true) GregorianCalendar = gregorianCalendar;
        if (status is not null && Status?.Equals(status, StringComparison.OrdinalIgnoreCase) is not true) Status = status;

        this.QueueDomainEvent(new AcademicYearUpdated() { AcademicYear = this });
        return this;
    }

    public static AcademicYear Update(Guid id, string ethCalendar, string gregorianCalendar, string status)
    {
        var academicYear = new AcademicYear
        {
            Id = id,
            EthCalendar = ethCalendar,
            GregorianCalendar = gregorianCalendar,
            Status = status
        };

        academicYear.QueueDomainEvent(new AcademicYearUpdated() { AcademicYear = academicYear });

        return academicYear;
    }
}
