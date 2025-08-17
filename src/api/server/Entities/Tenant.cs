using System;
using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.Api.Entities;

public class Tenant
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(256)] public string Name { get; set; } = default!;
    [MaxLength(32)] public string? WhatsAppNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
