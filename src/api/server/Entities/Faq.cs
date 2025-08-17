using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSH.Starter.Api.Entities;

public class Faq
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid TenantId { get; set; }
    [ForeignKey(nameof(TenantId))] public Tenant? Tenant { get; set; }
    [Required, MaxLength(1024)] public string Question { get; set; } = default!;
    [Required, MaxLength(4000)] public string Answer { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
