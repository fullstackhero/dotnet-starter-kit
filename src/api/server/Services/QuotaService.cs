using System;
using System.Linq;
using System.Threading.Tasks;
using FSH.Starter.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Starter.Api.Services;

public class QuotaService : IQuotaService
{
    private readonly AppDbContext _db;
    public QuotaService(AppDbContext db) => _db = db;

    public async Task AssertCanConsumeAsync(Guid tenantId)
    {
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId);
        if (sub is null || !sub.IsActive) throw new InvalidOperationException("No active subscription.");
        if (sub.UsedQuota >= sub.MonthlyQuota) throw new InvalidOperationException("Quota exceeded. Please upgrade your plan.");
    }

    public async Task ConsumeAsync(Guid tenantId)
    {
        var sub = await _db.Subscriptions.FirstAsync(s => s.TenantId == tenantId);
        sub.UsedQuota += 1;
        await _db.SaveChangesAsync();
    }
}
