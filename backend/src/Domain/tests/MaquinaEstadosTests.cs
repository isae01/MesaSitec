using Domain.Entities;
using Domain.Servicios;
using Xunit;

namespace Domain.Tests;

public class MaquinaEstadosTests
{
    [Fact]
    public void Nueva_Puede_Asignarse()
    {
        Assert.True(MaquinaEstados.EsValida(EstadoSolicitud.Nueva, "asignar"));
    }

    [Fact]
    public void Nueva_No_Puede_Resolverse_Directamente()
    {
        Assert.False(MaquinaEstados.EsValida(EstadoSolicitud.Nueva, "resolver"));
    }

    [Fact]
    public void Asignar_Mueve_De_Nueva_A_Asignada()
    {
        Assert.Equal(EstadoSolicitud.Asignada, MaquinaEstados.SiguienteEstado(EstadoSolicitud.Nueva, "asignar"));
    }

    [Fact]
    public void Cerrada_Es_Estado_Final_Sin_Acciones()
    {
        Assert.False(MaquinaEstados.EsValida(EstadoSolicitud.Cerrada, "reabrir"));
    }

    [Fact]
    public void Resuelta_Puede_Reabrirse_A_EnProceso()
    {
        Assert.Equal(EstadoSolicitud.EnProceso, MaquinaEstados.SiguienteEstado(EstadoSolicitud.Resuelta, "reabrir"));
    }
}