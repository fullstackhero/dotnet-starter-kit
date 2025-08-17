using System;
using System.Threading.Tasks;
using FSH.Starter.Api.Data;
using FSH.Starter.Api.Dtos;
using FSH.Starter.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FSH.Starter.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SubscriptionsController(AppDbContext db) => _db = db;

    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> Get(Guid tenantId)
        => Ok(await _db.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId));

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] SubscriptionUpsertDto dto)
    {
        var existing = await _db.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == dto.TenantId);
        if (existing is null)
        {
            existing = new Subscription { TenantId = dto.TenantId, Plan = dto.Plan, MonthlyQuota = dto.MonthlyQuota, UsedQuota = 0 };
            _db.Subscriptions.Add(existing);
        }
        else
        {
            existing.Plan = dto.Plan; existing.MonthlyQuota = dto.MonthlyQuota;
        }
        await _db.SaveChangesAsync(); return Ok(existing);
    }
}
