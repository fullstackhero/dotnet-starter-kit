using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSH.Starter.Api.Entities;

public class Subscription
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid TenantId { get; set; }
    [ForeignKey(nameof(TenantId))] public Tenant? Tenant { get; set; }
    [Required, MaxLength(64)] public string Plan { get; set; } = "Basic";
    public int MonthlyQuota { get; set; } = 1000;
    public int UsedQuota { get; set; } = 0;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public bool IsActive => EndDate == null || EndDate > DateTime.UtcNow;
}
