using Domain.Entities;

namespace Domain.Servicios;

public static class MaquinaEstados
{
    private static readonly Dictionary<EstadoSolicitud, Dictionary<string, EstadoSolicitud>> Transiciones = new()
    {
        [EstadoSolicitud.Nueva] = new()
        {
            ["asignar"] = EstadoSolicitud.Asignada,
            ["cancelar"] = EstadoSolicitud.Cancelada
        },
        [EstadoSolicitud.Asignada] = new()
        {
            ["iniciar"] = EstadoSolicitud.EnProceso,
            ["asignar"] = EstadoSolicitud.Asignada,
            ["cancelar"] = EstadoSolicitud.Cancelada
        },
        [EstadoSolicitud.EnProceso] = new()
        {
            ["resolver"] = EstadoSolicitud.Resuelta,
            ["asignar"] = EstadoSolicitud.Asignada,
            ["cancelar"] = EstadoSolicitud.Cancelada
        },
        [EstadoSolicitud.Resuelta] = new()
        {
            ["cerrar"] = EstadoSolicitud.Cerrada,
            ["reabrir"] = EstadoSolicitud.EnProceso
        },
        [EstadoSolicitud.Cerrada] = new(),
        [EstadoSolicitud.Cancelada] = new(),
    };

    public static bool EsValida(EstadoSolicitud estadoActual, string accion) =>
        Transiciones[estadoActual].ContainsKey(accion);

    public static EstadoSolicitud SiguienteEstado(EstadoSolicitud estadoActual, string accion) =>
        Transiciones[estadoActual][accion];
}