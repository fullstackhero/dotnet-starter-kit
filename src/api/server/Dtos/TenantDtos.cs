using System;
using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.Api.Dtos;

public class TenantCreateDto
{
    [Required, MaxLength(256)] public string Name { get; set; } = default!;
    [MaxLength(32)] public string? WhatsAppNumber { get; set; }
}
public class TenantUpdateDto : TenantCreateDto
{
    [Required] public Guid Id { get; set; }
}
