using System;
using System.Linq;
using System.Threading.Tasks;
using FSH.Starter.Api.Data;
using FSH.Starter.Api.Dtos;
using FSH.Starter.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FSH.Starter.Api.Controllers;

[ApiController]
[Route("api/faqs")]
public class FaqsController : ControllerBase
{
    private readonly AppDbContext _db;
    public FaqsController(AppDbContext db) => _db = db;

    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> GetByTenant(Guid tenantId)
        => Ok(await _db.Faqs.Where(f => f.TenantId == tenantId).OrderBy(f => f.Question).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FaqCreateDto dto)
    {
        var entity = new Faq { TenantId = dto.TenantId, Question = dto.Question, Answer = dto.Answer };
        _db.Faqs.Add(entity); await _db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FaqUpdateDto dto)
    {
        var entity = await _db.Faqs.FindAsync(id);
        if (entity is null) return NotFound();
        entity.Question = dto.Question; entity.Answer = dto.Answer;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Faqs.FindAsync(id);
        if (entity is null) return NotFound();
        _db.Faqs.Remove(entity); await _db.SaveChangesAsync();
        return NoContent();
    }
}
