using System;
using NeuroMock.Core.Processamento;
using Xunit;

namespace NeuroMock.Tests.Processamento;

public sealed class FftTests
{
    [Fact]
    public void ConfereComADftDireta()
    {
        const int n = 64;
        var aleatorio = new Random(7);
        var re = new double[n];
        var im = new double[n];
        for (var i = 0; i < n; i++)
        {
            re[i] = aleatorio.NextDouble() - 0.5;
            im[i] = aleatorio.NextDouble() - 0.5;
        }

        var (esperadoRe, esperadoIm) = DftDireta(re, im);
        Fft.Transformar(re, im);

        for (var k = 0; k < n; k++)
        {
            Assert.Equal(esperadoRe[k], re[k], 1e-9);
            Assert.Equal(esperadoIm[k], im[k], 1e-9);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(100)]
    public void TamanhoQueNaoEhPotenciaDeDoisLancaExcecao(int tamanho)
    {
        Assert.Throws<ArgumentException>(() => Fft.Transformar(new double[tamanho], new double[tamanho]));
    }

    [Fact]
    public void PartesComTamanhosDiferentesLancamExcecao()
    {
        Assert.Throws<ArgumentException>(() => Fft.Transformar(new double[8], new double[16]));
    }

    private static (double[] Re, double[] Im) DftDireta(double[] re, double[] im)
    {
        var n = re.Length;
        var saidaRe = new double[n];
        var saidaIm = new double[n];
        for (var k = 0; k < n; k++)
        {
            for (var t = 0; t < n; t++)
            {
                var angulo = -2 * Math.PI * k * t / n;
                saidaRe[k] += re[t] * Math.Cos(angulo) - im[t] * Math.Sin(angulo);
                saidaIm[k] += re[t] * Math.Sin(angulo) + im[t] * Math.Cos(angulo);
            }
        }

        return (saidaRe, saidaIm);
    }
}
