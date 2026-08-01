using Domain.Entities;

namespace Domain.Servicios;

public static class CalculadoraSla
{
    private static readonly Dictionary<Prioridad, double> Factor = new()
    {
        { Prioridad.Critica, 0.5 },
        { Prioridad.Alta, 0.75 },
        { Prioridad.Media, 1.0 },
        { Prioridad.Baja, 2.0 }
    };

    public static DateTime CalcularFechaLimite(DateTime fechaCreacion, int slaHoras, Prioridad prioridad)
    {
        var horasAjustadas = slaHoras * Factor[prioridad];
        return fechaCreacion.AddHours(horasAjustadas);
    }

    public static bool EstaVencida(DateTime fechaLimiteSla, EstadoSolicitud estado)
    {
        var estadosFinales = new[] { EstadoSolicitud.Resuelta, EstadoSolicitud.Cerrada, EstadoSolicitud.Cancelada };
        return fechaLimiteSla < DateTime.UtcNow && !estadosFinales.Contains(estado);
    }
}