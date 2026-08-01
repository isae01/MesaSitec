using Domain.Entities;

namespace Application.Dtos;

public class CrearSolicitudRequest
{
    public string Titulo { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public Guid CategoriaId { get; set; }
    public Prioridad Prioridad { get; set; }
}