//Equivalencia Prisma 
// model Usuario {
//   id Int @id
//   nombre String
//   email String
//   passwordHash String
// }
namespace Domain.Entities; // <-- Esta es su "dirección virtual"

// class = igual que un modelo de Prisma o una interface de TS, pero además EF Core
// lee esta clase para crear la tabla en la base de datos automáticamente
public class Usuario// <-- El 'public' es el verdadero 'export'. Tabla Usuario
{
    public Guid Id { get; set; }          // Guid = tipo especial para IDs únicos (como cuid()/uuid() en Prisma)
    public Guid TenantId { get; set; }    // FK a la tabla Tenant (como tenantId en tu schema de Prisma)
    public string Email { get; set; } = default!;
    // { get; set; } = "propiedad": se puede leer y escribir, como un campo normal de objeto en JS
    // = default! le dice al compilador "sé que esto no es null, confía en mí" (evita warnings de Nullable)

    public string PasswordHash { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public Rol Rol { get; set; }          // usa el enum de arriba — como rol: Rol en Prisma
    public bool Activo { get; set; } = true;  // = true es el valor por defecto (como @default(true) en Prisma)
}