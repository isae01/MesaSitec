using Api.Auth;
using Application.Dtos;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Domain.Servicios;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/solicitudes")]
[Authorize]
public class SolicitudesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SolicitudesController(AppDbContext db)
    {
        _db = db;
    }

[HttpGet]
public async Task<IActionResult> Listar(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? estado = null,
    [FromQuery] string? prioridad = null,
    [FromQuery] Guid? categoriaId = null,
    [FromQuery] Guid? agenteId = null,
    [FromQuery] string? q = null,
    [FromQuery] bool? vencidas = null,
    [FromQuery] string sort = "-fechaCreacion")
{
    if (page < 1 || pageSize > 100 || pageSize < 1)
    {
        return BadRequest(new
        {
            type = "https://mesasitec.local/errores/parametro-invalido",
            title = "Parámetro inválido", status = 400,
            detail = "page debe ser >= 1 y pageSize debe estar entre 1 y 100.",
            codigo = "PARAMETRO_INVALIDO"
        });
    }

    var tenantId = User.TenantId();

    var query = _db.Solicitudes.Where(s => s.TenantId == tenantId).Include(s => s.Categoria).AsQueryable();

    if (!string.IsNullOrEmpty(estado) && Enum.TryParse<Domain.Entities.EstadoSolicitud>(estado, out var estadoEnum))
        query = query.Where(s => s.Estado == estadoEnum);

    if (!string.IsNullOrEmpty(prioridad) && Enum.TryParse<Domain.Entities.Prioridad>(prioridad, out var prioridadEnum))
        query = query.Where(s => s.Prioridad == prioridadEnum);

    if (categoriaId.HasValue)
        query = query.Where(s => s.CategoriaId == categoriaId.Value);

    if (agenteId.HasValue)
        query = query.Where(s => s.AgenteId == agenteId.Value);

    if (!string.IsNullOrEmpty(q))
    {
        var qLower = q.ToLower();
        query = query.Where(s => s.Titulo.ToLower().Contains(qLower)
            || s.Descripcion.ToLower().Contains(qLower)
            || s.Codigo.ToLower().Contains(qLower));
    }

    var estadosFinales = new[] {
        Domain.Entities.EstadoSolicitud.Resuelta,
        Domain.Entities.EstadoSolicitud.Cerrada,
        Domain.Entities.EstadoSolicitud.Cancelada
    };
    if (vencidas == true)
        query = query.Where(s => s.FechaLimiteSla < DateTime.UtcNow && !estadosFinales.Contains(s.Estado));

    // Orden: para "prioridad" el enum ya está declarado Baja=0..Critica=3, así que ordenar por el
    // número da orden alfabético-de-severidad al revés; invertimos manualmente para que sea semántico
    query = sort switch
    {
        "fechaCreacion" => query.OrderBy(s => s.FechaCreacion),
        "-fechaCreacion" => query.OrderByDescending(s => s.FechaCreacion),
        "prioridad" => query.OrderBy(s => s.Prioridad),
        "-prioridad" => query.OrderByDescending(s => s.Prioridad),
        "codigo" => query.OrderBy(s => s.Codigo),
        _ => query.OrderByDescending(s => s.FechaCreacion)
    };

    var total = await query.CountAsync();

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(s => new SolicitudListItemDto
        {
            Id = s.Id,
            Codigo = s.Codigo,
            Titulo = s.Titulo,
            Estado = s.Estado.ToString(),
            Prioridad = s.Prioridad.ToString(),
            Categoria = new CategoriaResumenDto { Id = s.CategoriaId, Nombre = s.Categoria!.Nombre },
            Agente = s.AgenteId == null ? null : new AgenteResumenDto
            {
                Id = s.AgenteId.Value,
                Nombre = _db.Usuarios.Where(u => u.Id == s.AgenteId).Select(u => u.Nombre).FirstOrDefault() ?? ""
            },
            FechaCreacion = s.FechaCreacion,
            FechaLimiteSla = s.FechaLimiteSla,
            Vencida = s.FechaLimiteSla < DateTime.UtcNow && !estadosFinales.Contains(s.Estado)
        })
        .ToListAsync();

    return Ok(new SolicitudesPaginadasDto
    {
        Items = items, Page = page, PageSize = pageSize, Total = total,
        TotalPaginas = (int)Math.Ceiling(total / (double)pageSize)
    });
}

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearSolicitudRequest body)
    {
        var tenantId = User.TenantId();
        var userId = User.UserId();

        if (body.Titulo.Length < 5 || body.Titulo.Length > 120 ||
            body.Descripcion.Length < 10 || body.Descripcion.Length > 4000)
        {
            return UnprocessableEntity(new
            {
                type = "https://mesasitec.local/errores/validacion",
                title = "Error de validación",
                status = 422,
                detail = "Revisa los campos.",
                codigo = "VALIDACION",
                errores = new Dictionary<string, string[]>
                {
                    ["titulo"] = body.Titulo.Length < 5 || body.Titulo.Length > 120
                        ? new[] { "El título debe tener entre 5 y 120 caracteres." } : Array.Empty<string>()
                }
            });
        }

        var categoria = await _db.Categorias
            .FirstOrDefaultAsync(c => c.Id == body.CategoriaId && c.TenantId == tenantId);

        if (categoria == null)
        {
            return NotFound(new
            {
                type = "https://mesasitec.local/errores/recurso-no-encontrado",
                title = "Recurso no encontrado",
                status = 404,
                detail = "La categoría no existe.",
                codigo = "RECURSO_NO_ENCONTRADO"
            });
        }

        var anioActual = DateTime.UtcNow.Year;
        var cantidadEsteAnio = await _db.Solicitudes
            .CountAsync(s => s.TenantId == tenantId && s.FechaCreacion.Year == anioActual);
        var correlativo = (cantidadEsteAnio + 1).ToString("D5");
        var codigo = $"SOL-{anioActual}-{correlativo}";

        var fechaCreacion = DateTime.UtcNow;
        var fechaLimite = CalculadoraSla.CalcularFechaLimite(
            fechaCreacion, categoria.SlaHoras, body.Prioridad);

        var solicitud = new Domain.Entities.Solicitud
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Codigo = codigo,
            Titulo = body.Titulo,
            Descripcion = body.Descripcion,
            CategoriaId = body.CategoriaId,
            Prioridad = body.Prioridad,
            Estado = Domain.Entities.EstadoSolicitud.Nueva,
            SolicitanteId = userId,
            FechaCreacion = fechaCreacion,
            FechaLimiteSla = fechaLimite
        };

        _db.Solicitudes.Add(solicitud);
        await _db.SaveChangesAsync();

        Response.Headers.Location = $"/api/v1/solicitudes/{solicitud.Id}";

        return StatusCode(201, new
        {
            id = solicitud.Id,
            codigo = solicitud.Codigo,
            titulo = solicitud.Titulo,
            estado = solicitud.Estado.ToString(),
            prioridad = solicitud.Prioridad.ToString(),
            fechaCreacion = solicitud.FechaCreacion,
            fechaLimiteSla = solicitud.FechaLimiteSla
        });
    }

    [HttpGet("{id}")]
public async Task<IActionResult> Detalle(Guid id)
{
    var tenantId = User.TenantId();
    var rol = User.Rol();

    var s = await _db.Solicitudes
        .Include(x => x.Categoria)
        .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

    if (s == null)
    {
        return NotFound(new
        {
            type = "https://mesasitec.local/errores/recurso-no-encontrado",
            title = "Recurso no encontrado",
            status = 404,
            detail = "La solicitud no existe.",
            codigo = "RECURSO_NO_ENCONTRADO"
        });
    }

    // RN-03: Solicitante solo puede ver las propias
    if (rol == "Solicitante" && s.SolicitanteId != User.UserId())
    {
        return NotFound(new
        {
            type = "https://mesasitec.local/errores/recurso-no-encontrado",
            title = "Recurso no encontrado",
            status = 404,
            detail = "La solicitud no existe.",
            codigo = "RECURSO_NO_ENCONTRADO"
        });
    }

    var solicitante = await _db.Usuarios.FirstAsync(u => u.Id == s.SolicitanteId);
    var agente = s.AgenteId == null ? null : await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == s.AgenteId);

    return Ok(new
    {
        id = s.Id,
        codigo = s.Codigo,
        titulo = s.Titulo,
        descripcion = s.Descripcion,
        estado = s.Estado.ToString(),
        prioridad = s.Prioridad.ToString(),
        categoria = new { id = s.CategoriaId, nombre = s.Categoria!.Nombre },
        agente = agente == null ? null : new { id = agente.Id, nombre = agente.Nombre },
        solicitante = new { id = solicitante.Id, nombre = solicitante.Nombre },
        fechaCreacion = s.FechaCreacion,
        fechaLimiteSla = s.FechaLimiteSla,
        fechaResolucion = s.FechaResolucion,
        motivoResolucion = s.MotivoResolucion,
        motivoCancelacion = s.MotivoCancelacion,
        vencida = s.FechaLimiteSla < DateTime.UtcNow
            && s.Estado != Domain.Entities.EstadoSolicitud.Resuelta
            && s.Estado != Domain.Entities.EstadoSolicitud.Cerrada
            && s.Estado != Domain.Entities.EstadoSolicitud.Cancelada
    });
}

[HttpPut("{id}")]
public async Task<IActionResult> Editar(Guid id, [FromBody] CrearSolicitudRequest body)
{
    var tenantId = User.TenantId();
    var rol = User.Rol();

    var s = await _db.Solicitudes.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
    if (s == null)
    {
        return NotFound(new
        {
            type = "https://mesasitec.local/errores/recurso-no-encontrado",
            title = "Recurso no encontrado", status = 404,
            detail = "La solicitud no existe.", codigo = "RECURSO_NO_ENCONTRADO"
        });
    }

    // RN-03: Solicitante solo edita las propias y solo en estado Nueva
    if (rol == "Solicitante")
    {
        var esPropia = s.SolicitanteId == User.UserId();
        if (!esPropia || s.Estado != Domain.Entities.EstadoSolicitud.Nueva)
        {
            return StatusCode(403, new
            {
                type = "https://mesasitec.local/errores/operacion-no-permitida",
                title = "Operación no permitida", status = 403,
                detail = "Solo puedes editar tus propias solicitudes mientras estén en estado Nueva.",
                codigo = "OPERACION_NO_PERMITIDA"
            });
        }
    }

    if (body.Titulo.Length < 5 || body.Titulo.Length > 120 ||
        body.Descripcion.Length < 10 || body.Descripcion.Length > 4000)
    {
        return UnprocessableEntity(new
        {
            type = "https://mesasitec.local/errores/validacion",
            title = "Error de validación", status = 422,
            detail = "Revisa los campos.", codigo = "VALIDACION"
        });
    }

    var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == body.CategoriaId && c.TenantId == tenantId);
    if (categoria == null)
    {
        return NotFound(new
        {
            type = "https://mesasitec.local/errores/recurso-no-encontrado",
            title = "Recurso no encontrado", status = 404,
            detail = "La categoría no existe.", codigo = "RECURSO_NO_ENCONTRADO"
        });
    }

    // RN-04: si cambia categoría o prioridad y no está resuelta, recalculamos el SLA
    var estadosFinales = new[] {
        Domain.Entities.EstadoSolicitud.Resuelta,
        Domain.Entities.EstadoSolicitud.Cerrada,
        Domain.Entities.EstadoSolicitud.Cancelada
    };
    var cambioRelevante = s.CategoriaId != body.CategoriaId || s.Prioridad != body.Prioridad;
    if (cambioRelevante && !estadosFinales.Contains(s.Estado))
    {
        s.FechaLimiteSla = CalculadoraSla.CalcularFechaLimite(s.FechaCreacion, categoria.SlaHoras, body.Prioridad);
    }

    s.Titulo = body.Titulo;
    s.Descripcion = body.Descripcion;
    s.CategoriaId = body.CategoriaId;
    s.Prioridad = body.Prioridad;

    await _db.SaveChangesAsync();

    return Ok(new
    {
        id = s.Id, codigo = s.Codigo, titulo = s.Titulo, descripcion = s.Descripcion,
        categoriaId = s.CategoriaId, prioridad = s.Prioridad.ToString(),
        estado = s.Estado.ToString(), fechaLimiteSla = s.FechaLimiteSla
    });
}

    [HttpPost("{id}/transiciones")]
    public async Task<IActionResult> EjecutarTransicion(Guid id, [FromBody] TransicionRequest body)
    {
        var tenantId = User.TenantId();
        var rol = User.Rol();

        var solicitud = await _db.Solicitudes
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);

        if (solicitud == null)
        {
            return NotFound(new
            {
                type = "https://mesasitec.local/errores/recurso-no-encontrado",
                title = "Recurso no encontrado",
                status = 404,
                detail = "La solicitud no existe.",
                codigo = "RECURSO_NO_ENCONTRADO"
            });
        }

        var accionesPermitidasSolicitante = new[] { "cerrar" };
        if (rol == "Solicitante")
        {
            var esPropia = solicitud.SolicitanteId == User.UserId();
            if (!esPropia || !accionesPermitidasSolicitante.Contains(body.Accion))
            {
                return StatusCode(403, new
                {
                    type = "https://mesasitec.local/errores/operacion-no-permitida",
                    title = "Operación no permitida",
                    status = 403,
                    detail = "Tu rol no permite ejecutar esta acción.",
                    codigo = "OPERACION_NO_PERMITIDA"
                });
            }
        }
        if (rol == "Agente" && body.Accion == "cancelar")
        {
            return StatusCode(403, new
            {
                type = "https://mesasitec.local/errores/operacion-no-permitida",
                title = "Operación no permitida",
                status = 403,
                detail = "Los agentes no pueden cancelar solicitudes.",
                codigo = "OPERACION_NO_PERMITIDA"
            });
        }

        if (!MaquinaEstados.EsValida(solicitud.Estado, body.Accion))
        {
            return Conflict(new
            {
                type = "https://mesasitec.local/errores/transicion-invalida",
                title = "Transición inválida",
                status = 409,
                detail = $"No se puede aplicar '{body.Accion}' sobre una solicitud en estado '{solicitud.Estado}'.",
                codigo = "TRANSICION_INVALIDA"
            });
        }

        if (body.Accion == "resolver" && (body.Motivo == null || body.Motivo.Length < 20))
        {
            return UnprocessableEntity(new
            {
                type = "https://mesasitec.local/errores/motivo-requerido",
                title = "Motivo requerido",
                status = 422,
                detail = "El motivo de resolución debe tener al menos 20 caracteres.",
                codigo = "MOTIVO_REQUERIDO"
            });
        }
        if (body.Accion == "cancelar" && (body.Motivo == null || body.Motivo.Length < 10))
        {
            return UnprocessableEntity(new
            {
                type = "https://mesasitec.local/errores/motivo-requerido",
                title = "Motivo requerido",
                status = 422,
                detail = "El motivo de cancelación debe tener al menos 10 caracteres.",
                codigo = "MOTIVO_REQUERIDO"
            });
        }

        if (body.Accion == "asignar")
        {
            if (body.AgenteId == null)
            {
                return UnprocessableEntity(new
                {
                    type = "https://mesasitec.local/errores/agente-invalido",
                    title = "Agente inválido",
                    status = 422,
                    detail = "Debes indicar un agenteId.",
                    codigo = "AGENTE_INVALIDO"
                });
            }

            var agente = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == body.AgenteId);
            var agenteValido = agente != null
                && agente.Activo
                && agente.TenantId == tenantId
                && (agente.Rol == Domain.Entities.Rol.Agente || agente.Rol == Domain.Entities.Rol.Admin);

            if (!agenteValido)
            {
                return UnprocessableEntity(new
                {
                    type = "https://mesasitec.local/errores/agente-invalido",
                    title = "Agente inválido",
                    status = 422,
                    detail = "El agente indicado no existe, está inactivo, o no pertenece a tu organización.",
                    codigo = "AGENTE_INVALIDO"
                });
            }

            solicitud.AgenteId = body.AgenteId;
        }

        solicitud.Estado = MaquinaEstados.SiguienteEstado(solicitud.Estado, body.Accion);

        if (body.Accion == "resolver")
        {
            solicitud.MotivoResolucion = body.Motivo;
            solicitud.FechaResolucion = DateTime.UtcNow;
        }
        if (body.Accion == "cancelar")
        {
            solicitud.MotivoCancelacion = body.Motivo;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = solicitud.Id,
            codigo = solicitud.Codigo,
            estado = solicitud.Estado.ToString(),
            agenteId = solicitud.AgenteId,
            fechaResolucion = solicitud.FechaResolucion,
            motivoResolucion = solicitud.MotivoResolucion,
            motivoCancelacion = solicitud.MotivoCancelacion
        });
    }
}