namespace Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = default!;
    public bool Activo { get; set; } = true;
}