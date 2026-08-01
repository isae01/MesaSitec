namespace Domain.Entities;

public class Solicitud
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Codigo { get; set; } = default!;
    public string Titulo { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public Guid CategoriaId { get; set; }
    public Categoria? Categoria { get; set; } // propiedad de navegación — como el "include" de Prisma
    public Prioridad Prioridad { get; set; }
    public EstadoSolicitud Estado { get; set; }
    public Guid SolicitanteId { get; set; }
    public Guid? AgenteId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaLimiteSla { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public string? MotivoResolucion { get; set; }
    public string? MotivoCancelacion { get; set; }
}