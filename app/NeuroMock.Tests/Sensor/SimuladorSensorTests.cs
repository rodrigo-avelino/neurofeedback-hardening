using System.Linq;
using NeuroMock.Core.Sensor;
using Xunit;

namespace NeuroMock.Tests.Sensor;

public sealed class SimuladorSensorTests
{
    [Fact]
    public void MesmaSementeDaAsMesmasAmostras()
    {
        Assert.Equal(new SimuladorSensor(semente: 7).Ler(1000), new SimuladorSensor(semente: 7).Ler(1000));
    }

    [Fact]
    public void SementesDiferentesDivergem()
    {
        Assert.NotEqual(new SimuladorSensor(semente: 7).Ler(1000), new SimuladorSensor(semente: 8).Ler(1000));
    }

    [Fact]
    public void LerEmPartesContinuaDeOndeParou()
    {
        var emPartes = new SimuladorSensor(semente: 7);
        var juntas = emPartes.Ler(300).Concat(emPartes.Ler(700)).ToArray();
        Assert.Equal(new SimuladorSensor(semente: 7).Ler(1000), juntas);
    }
}
