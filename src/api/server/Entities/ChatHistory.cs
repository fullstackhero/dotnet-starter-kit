using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSH.Starter.Api.Entities;

public class ChatHistory
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid TenantId { get; set; }
    [ForeignKey(nameof(TenantId))] public Tenant? Tenant { get; set; }
    [Required] public string UserMessage { get; set; } = default!;
    public string? BotResponse { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
