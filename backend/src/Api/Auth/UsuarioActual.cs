using System.Security.Claims;

namespace Api.Auth;

// "Extension method" — agrega un método nuevo a ClaimsPrincipal (el "User" de cada controller)
// Es como una función utilitaria que reutilizas en Node: getUserFromReq(req)
public static class UsuarioActualExtensions
{
    public static Guid TenantId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirst("tenantId")!.Value);

    public static Guid UserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")!.Value);

    public static string Rol(this ClaimsPrincipal user) =>
        user.FindFirst("rol")!.Value;
}