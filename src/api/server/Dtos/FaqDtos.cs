using System;
using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.Api.Dtos;

public class FaqCreateDto
{
    [Required] public Guid TenantId { get; set; }
    [Required, MaxLength(1024)] public string Question { get; set; } = default!;
    [Required, MaxLength(4000)] public string Answer { get; set; } = default!;
}
public class FaqUpdateDto : FaqCreateDto
{
    [Required] public Guid Id { get; set; }
}
