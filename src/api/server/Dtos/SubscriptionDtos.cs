using System;
using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.Api.Dtos;

public class SubscriptionUpsertDto
{
    [Required] public Guid TenantId { get; set; }
    [Required, MaxLength(64)] public string Plan { get; set; } = "Basic";
    public int MonthlyQuota { get; set; } = 1000;
}
