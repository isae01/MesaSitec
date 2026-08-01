using Domain.Entities;
using Domain.Servicios;
using Xunit;

namespace Domain.Tests;

public class CalculadoraSlaTests
{
    [Fact]
    public void Prioridad_Critica_Reduce_El_Sla_A_La_Mitad()
    {
        var creacion = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);
        var limite = CalculadoraSla.CalcularFechaLimite(creacion, slaHoras: 8, Prioridad.Critica);
        Assert.Equal(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), limite);
    }

    [Fact]
    public void Prioridad_Baja_Duplica_El_Sla()
    {
        var creacion = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);
        var limite = CalculadoraSla.CalcularFechaLimite(creacion, slaHoras: 24, Prioridad.Baja);
        Assert.Equal(new DateTime(2026, 1, 17, 8, 0, 0, DateTimeKind.Utc), limite);
    }

    [Fact]
    public void Solicitud_Vencida_Si_Paso_La_Fecha_Y_No_Esta_Resuelta()
    {
        Assert.True(CalculadoraSla.EstaVencida(DateTime.UtcNow.AddHours(-1), EstadoSolicitud.EnProceso));
    }

    [Fact]
    public void Solicitud_No_Vencida_Si_Ya_Esta_Resuelta_Aunque_Paso_La_Fecha()
    {
        Assert.False(CalculadoraSla.EstaVencida(DateTime.UtcNow.AddHours(-1), EstadoSolicitud.Resuelta));
    }
}