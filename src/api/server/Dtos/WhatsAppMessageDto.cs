using System;
namespace FSH.Starter.Api.Dtos;

public class WhatsAppMessageDto
{
    public Guid TenantId { get; set; }
    public string From { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string? MessageId { get; set; }
}
