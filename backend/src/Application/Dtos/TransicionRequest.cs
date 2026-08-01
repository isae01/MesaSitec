namespace Application.Dtos;

// El body puede traer distintos campos según la acción (agenteId para asignar, motivo para resolver/cancelar)
// Por eso todos son nullable — como un objeto con campos opcionales en TS
public class TransicionRequest
{
    public string Accion { get; set; } = default!;
    public Guid? AgenteId { get; set; }
    public string? Motivo { get; set; }
}