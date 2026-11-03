using System;

namespace NeuroMock.Core.Processamento;

/// <summary>
/// Filtro IIR de segunda ordem (forma direta I), com coeficientes do Audio EQ Cookbook de
/// Robert Bristow-Johnson. Processa amostra a amostra e guarda o próprio estado.
/// </summary>
public sealed class FiltroBiquad
{
    private const double QButterworth = 0.7071067811865476;

    private readonly double _b0, _b1, _b2, _a1, _a2;
    private double _x1, _x2, _y1, _y2;

    private FiltroBiquad(double b0, double b1, double b2, double a0, double a1, double a2)
    {
        _b0 = b0 / a0;
        _b1 = b1 / a0;
        _b2 = b2 / a0;
        _a1 = a1 / a0;
        _a2 = a2 / a0;
    }

    public static FiltroBiquad PassaAltas(double taxaAmostragem, double corte, double q = QButterworth)
    {
        var (cos, alfa) = Prototipo(taxaAmostragem, corte, q);
        return new FiltroBiquad((1 + cos) / 2, -(1 + cos), (1 + cos) / 2, 1 + alfa, -2 * cos, 1 - alfa);
    }

    /// <summary>Rejeita-faixa estreito (notch), usado para o ruído da rede elétrica.</summary>
    public static FiltroBiquad Rejeita(double taxaAmostragem, double frequencia, double q)
    {
        var (cos, alfa) = Prototipo(taxaAmostragem, frequencia, q);
        return new FiltroBiquad(1, -2 * cos, 1, 1 + alfa, -2 * cos, 1 - alfa);
    }

    public double Processar(double x)
    {
        var y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
        _x2 = _x1;
        _x1 = x;
        _y2 = _y1;
        _y1 = y;
        return y;
    }

    private static (double Cos, double Alfa) Prototipo(double taxaAmostragem, double frequencia, double q)
    {
        var w0 = 2 * Math.PI * frequencia / taxaAmostragem;
        return (Math.Cos(w0), Math.Sin(w0) / (2 * q));
    }
}
