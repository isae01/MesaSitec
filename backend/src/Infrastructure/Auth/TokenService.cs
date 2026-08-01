using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth;

// Esto es literalmente tu jwt.sign({...}, secret, {expiresIn}) de Node, pero en C#
public class TokenService
{
    private readonly string _secret;
    private readonly int _horas;

    public TokenService(string secret, int horas)
    {
        _secret = secret;
        _horas = horas;
    }

    public string GenerarToken(Usuario usuario)
    {
        // Los "claims" son el payload del token — igual que { sub, tenantId, rol, email } en tu jwt.sign
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim("tenantId", usuario.TenantId.ToString()),
            new Claim("rol", usuario.Rol.ToString()),
            new Claim("email", usuario.Email),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // HS256, como pide el enunciado

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_horas),
            signingCredentials: credenciales
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}