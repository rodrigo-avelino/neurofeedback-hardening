using System;
using NeuroMock.Core.Processamento;
using Xunit;

namespace NeuroMock.Tests.Processamento;

public sealed class FiltroBiquadTests
{
    private const double Taxa = 250;

    [Fact]
    public void NotchAtenuaARedeEmMaisDe20dB()
    {
        var notch = FiltroBiquad.Rejeita(Taxa, 60, 30);
        Assert.True(GanhoEmDb(notch, 60) < -20);
    }

    [Fact]
    public void NotchPreservaAlfa()
    {
        var notch = FiltroBiquad.Rejeita(Taxa, 60, 30);
        Assert.True(GanhoEmDb(notch, 10) > -0.5);
    }

    [Fact]
    public void PassaAltasPreservaTeta()
    {
        var passaAltas = FiltroBiquad.PassaAltas(Taxa, 1.0);
        Assert.True(GanhoEmDb(passaAltas, 6) > -0.5);
    }

    [Fact]
    public void PassaAltasRemoveNivelContinuo()
    {
        var passaAltas = FiltroBiquad.PassaAltas(Taxa, 1.0);
        var saida = 0.0;
        for (var i = 0; i < 5000; i++)
            saida = passaAltas.Processar(5.0);
        Assert.True(Math.Abs(saida) < 1e-3);
    }

    // Ganho em regime: descarta a primeira metade (transitório) e compara o RMS com o do seno.
    private static double GanhoEmDb(FiltroBiquad filtro, double frequencia)
    {
        const int amostras = 5000;
        const int descartar = 2500;
        var soma = 0.0;
        for (var i = 0; i < amostras; i++)
        {
            var y = filtro.Processar(Math.Sin(2 * Math.PI * frequencia * i / Taxa));
            if (i >= descartar)
                soma += y * y;
        }

        var rms = Math.Sqrt(soma / (amostras - descartar));
        return 20 * Math.Log10(rms / Math.Sqrt(0.5));
    }
}
