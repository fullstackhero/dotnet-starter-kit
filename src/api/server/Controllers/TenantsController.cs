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
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly AppDbContext _db;
    public TenantsController(AppDbContext db) => _db = db;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _db.Tenants.OrderBy(x => x.Name).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TenantCreateDto dto)
    {
        var entity = new Tenant { Name = dto.Name, WhatsAppNumber = dto.WhatsAppNumber };
        _db.Tenants.Add(entity); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) => Ok(await _db.Tenants.FindAsync(id));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TenantUpdateDto dto)
    {
        var entity = await _db.Tenants.FindAsync(id);
        if (entity is null) return NotFound();
        entity.Name = dto.Name; entity.WhatsAppNumber = dto.WhatsAppNumber;
        await _db.SaveChangesAsync(); return NoContent();
    }
}
