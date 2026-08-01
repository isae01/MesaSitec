using Domain.Entities;

namespace Infrastructure.Data;

// Como tu seed.ts de Prisma: llena la base de datos SOLO si está vacía
public static class DataSeeder
{
    public static void Seed(AppDbContext context)
    {
        if (context.Tenants.Any()) return; // ya hay datos, no dupliques

        // Fecha fija de referencia (nunca DateTime.UtcNow, para que el seed sea siempre igual)
        var fechaBaseStr = Environment.GetEnvironmentVariable("SEED_FECHA_BASE") ?? "2026-01-15T08:00:00Z";
        var fechaBase = DateTime.Parse(fechaBaseStr).ToUniversalTime();

        // --- Tenants (organizaciones) ---
        var norte = new Tenant { Id = Guid.NewGuid(), Nombre = "Cooperativa Norte", Activo = true };
        var sur   = new Tenant { Id = Guid.NewGuid(), Nombre = "Bufete Sur", Activo = true };
        context.Tenants.AddRange(norte, sur);

        // --- Usuarios: misma contraseña para todos, hasheada con BCrypt (como bcrypt.hash en Node) ---
        var hash = BCrypt.Net.BCrypt.HashPassword("Sitec.2026");

        var adminNorte   = new Usuario { Id = Guid.NewGuid(), TenantId = norte.Id, Email = "admin@norte.test", Nombre = "Admin Norte", Rol = Rol.Admin, PasswordHash = hash };
        var agente1Norte = new Usuario { Id = Guid.NewGuid(), TenantId = norte.Id, Email = "agente1@norte.test", Nombre = "Agente Uno", Rol = Rol.Agente, PasswordHash = hash };
        var agente2Norte = new Usuario { Id = Guid.NewGuid(), TenantId = norte.Id, Email = "agente2@norte.test", Nombre = "Agente Dos", Rol = Rol.Agente, PasswordHash = hash };
        var user1Norte   = new Usuario { Id = Guid.NewGuid(), TenantId = norte.Id, Email = "user1@norte.test", Nombre = "Usuario Uno", Rol = Rol.Solicitante, PasswordHash = hash };
        var user2Norte   = new Usuario { Id = Guid.NewGuid(), TenantId = norte.Id, Email = "user2@norte.test", Nombre = "Usuario Dos", Rol = Rol.Solicitante, PasswordHash = hash };
        var adminSur     = new Usuario { Id = Guid.NewGuid(), TenantId = sur.Id, Email = "admin@sur.test", Nombre = "Admin Sur", Rol = Rol.Admin, PasswordHash = hash };
        var user1Sur     = new Usuario { Id = Guid.NewGuid(), TenantId = sur.Id, Email = "user1@sur.test", Nombre = "Usuario Sur", Rol = Rol.Solicitante, PasswordHash = hash };

        context.Usuarios.AddRange(adminNorte, agente1Norte, agente2Norte, user1Norte, user2Norte, adminSur, user1Sur);

        // --- Categorías (las mismas 4, repetidas en cada tenant) ---
        var catsNorte = CrearCategorias(norte.Id);
        var catsSur   = CrearCategorias(sur.Id);
        context.Categorias.AddRange(catsNorte);
        context.Categorias.AddRange(catsSur);

        // --- Solicitudes: 25 en Norte, 8 en Sur ---
        context.Solicitudes.AddRange(
            GenerarSolicitudes(norte.Id, catsNorte, new[] { user1Norte.Id, user2Norte.Id },
                new[] { agente1Norte.Id, agente2Norte.Id }, 25, fechaBase));

        context.Solicitudes.AddRange(
            GenerarSolicitudes(sur.Id, catsSur, new[] { user1Sur.Id },
                new[] { adminSur.Id }, 8, fechaBase));

        context.SaveChanges(); // como await prisma.$transaction([...]) al final
    }

    private static List<Categoria> CrearCategorias(Guid tenantId) => new()
    {
        new Categoria { Id = Guid.NewGuid(), TenantId = tenantId, Nombre = "Incidente", SlaHoras = 8 },
        new Categoria { Id = Guid.NewGuid(), TenantId = tenantId, Nombre = "Requerimiento", SlaHoras = 40 },
        new Categoria { Id = Guid.NewGuid(), TenantId = tenantId, Nombre = "Consulta", SlaHoras = 24 },
        new Categoria { Id = Guid.NewGuid(), TenantId = tenantId, Nombre = "Falla crítica", SlaHoras = 4 },
    };

    // Factor de RN-04: ajusta el SLA según la prioridad
    private static readonly Dictionary<Prioridad, double> Factor = new()
    {
        { Prioridad.Critica, 0.5 }, { Prioridad.Alta, 0.75 }, { Prioridad.Media, 1.0 }, { Prioridad.Baja, 2.0 }
    };

    private static List<Solicitud> GenerarSolicitudes(Guid tenantId, List<Categoria> categorias,
        Guid[] solicitantes, Guid[] agentes, int cantidad, DateTime fechaBase)
    {
        var estados = new[] { EstadoSolicitud.Nueva, EstadoSolicitud.Asignada, EstadoSolicitud.EnProceso,
            EstadoSolicitud.Resuelta, EstadoSolicitud.Cerrada, EstadoSolicitud.Cancelada };
        var prioridades = new[] { Prioridad.Baja, Prioridad.Media, Prioridad.Alta, Prioridad.Critica };

        var resultado = new List<Solicitud>();

        for (int i = 1; i <= cantidad; i++)
        {
            var estado = estados[i % estados.Length];
            var prioridad = prioridades[i % prioridades.Length];
            var categoria = categorias[i % categorias.Count];            
            var solicitante = solicitantes[i % solicitantes.Length];

            // Aseguramos al menos 5 vencidas y 3 resueltas en el tenant grande (Norte)
            if (cantidad >= 20 && i <= 5) estado = EstadoSolicitud.EnProceso;
            if (cantidad >= 20 && i > 5 && i <= 8) estado = EstadoSolicitud.Resuelta;

            var fechaCreacion = fechaBase.AddHours(-i * 3);
            var horasSla = categoria.SlaHoras * Factor[prioridad];
            var fechaLimite = fechaCreacion.AddHours(horasSla);

            // Forzamos que las 5 primeras ya hayan vencido respecto a la fecha base
            if (cantidad >= 20 && i <= 5) fechaLimite = fechaBase.AddHours(-1);

            var esResuelta = estado is EstadoSolicitud.Resuelta or EstadoSolicitud.Cerrada;

            resultado.Add(new Solicitud
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Codigo = $"SOL-{fechaCreacion.Year}-{i:D5}",
                Titulo = $"Solicitud de prueba número {i}",
                Descripcion = $"Descripción de la solicitud de prueba número {i} generada por el seed.",
                CategoriaId = categoria.Id,
                Prioridad = prioridad,
                Estado = estado,
                SolicitanteId = solicitante,
                AgenteId = estado == EstadoSolicitud.Nueva ? null : agentes[i % agentes.Length],
                FechaCreacion = fechaCreacion,
                FechaLimiteSla = fechaLimite,
                FechaResolucion = esResuelta ? fechaCreacion.AddHours(2) : null,
                MotivoResolucion = esResuelta ? "Resuelto durante la carga de datos semilla." : null,
                MotivoCancelacion = estado == EstadoSolicitud.Cancelada ? "Cancelada durante la carga de datos semilla." : null,
            });
        }

        return resultado;
    }
}