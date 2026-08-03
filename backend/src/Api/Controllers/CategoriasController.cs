using Api.Auth;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/categorias")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriasController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var tenantId = User.TenantId();

        var categorias = await _db.Categorias
            .Where(c => c.TenantId == tenantId && c.Activo)
            .Select(c => new { id = c.Id, nombre = c.Nombre, slaHoras = c.SlaHoras })
            .ToListAsync();

        return Ok(categorias);
    }
}