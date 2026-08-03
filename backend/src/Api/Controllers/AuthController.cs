using Application.Dtos;
using Infrastructure.Auth;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;

    // "inyección de dependencias" — como recibir (req, res, next) pero para servicios
    public AuthController(AppDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest body)
    {
        // Buscamos el usuario por email — como prisma.usuario.findUnique({ where: { email } })
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == body.Email && u.Activo);

        // Comparamos con BCrypt — igual que bcrypt.compare(password, hash) en Node
        if (usuario == null || !BCrypt.Net.BCrypt.Verify(body.Password, usuario.PasswordHash))
        {
            return Unauthorized(new
            {
                type = "https://mesasitec.local/errores/no-autenticado",
                title = "No autenticado",
                status = 401,
                detail = "Credenciales incorrectas.",
                codigo = "NO_AUTENTICADO"
            });
        }

        var tenant = await _db.Tenants.FirstAsync(t => t.Id == usuario.TenantId);
        var token = _tokenService.GenerarToken(usuario);

        return Ok(new
        {
            accessToken = token,
            expiraEn = 8 * 3600,
            usuario = new
            {
                id = usuario.Id,
                nombre = usuario.Nombre,
                email = usuario.Email,
                rol = usuario.Rol.ToString(),
                tenantId = usuario.TenantId,
                tenantNombre = tenant.Nombre
            }
        });
    }

    [HttpGet("/api/v1/me")]
    [Authorize] // exige que el token JWT sea válido — como tu middleware "requireAuth" en cada ruta
    public async Task<IActionResult> Me()
    {
        // User.FindFirst(...) lee los "claims" del token ya verificado por el middleware
        // Es como req.user.tenantId después de tu jwt.verify() en Express
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")!.Value);

        var usuario = await _db.Usuarios.FirstAsync(u => u.Id == userId);
        var tenant = await _db.Tenants.FirstAsync(t => t.Id == usuario.TenantId);

        return Ok(new
        {
            id = usuario.Id,
            nombre = usuario.Nombre,
            email = usuario.Email,
            rol = usuario.Rol.ToString(),
            tenantId = usuario.TenantId,
            tenantNombre = tenant.Nombre
        });
    }
}