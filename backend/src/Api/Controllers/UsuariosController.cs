using Api.Auth;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsuariosController(AppDbContext db) { _db = db; }

    // Endpoint adicional no pedido literal en el contrato, declarado en DECISIONES.md
    [HttpGet("agentes")]
    public async Task<IActionResult> ListarAgentes()
    {
        var tenantId = User.TenantId();
        var agentes = await _db.Usuarios
            .Where(u => u.TenantId == tenantId && u.Activo &&
                (u.Rol == Domain.Entities.Rol.Agente || u.Rol == Domain.Entities.Rol.Admin))
            .Select(u => new { id = u.Id, nombre = u.Nombre })
            .ToListAsync();
        return Ok(agentes);
    }
}