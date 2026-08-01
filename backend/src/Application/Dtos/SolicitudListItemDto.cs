namespace Application.Dtos;

// Esto es la forma exacta que pide el contrato de la API (sección 6.2, endpoint 4)
// Como definir un type en TypeScript para la respuesta de tu API
public class SolicitudListItemDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = default!;
    public string Titulo { get; set; } = default!;
    public string Estado { get; set; } = default!;
    public string Prioridad { get; set; } = default!;
    public CategoriaResumenDto Categoria { get; set; } = default!;
    public AgenteResumenDto? Agente { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaLimiteSla { get; set; }
    public bool Vencida { get; set; }
}

public class CategoriaResumenDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = default!;
}

public class AgenteResumenDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = default!;
}

public class SolicitudesPaginadasDto
{
    public List<SolicitudListItemDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPaginas { get; set; }
}