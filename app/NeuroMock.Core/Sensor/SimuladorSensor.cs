using System;

namespace NeuroMock.Core.Sensor;

/// <summary>Fonte de amostras de EEG de um canal, em microvolts. O sensor real vem pelo SDK do fabricante.</summary>
public interface IFonteSinal
{
    double TaxaAmostragem { get; }

    double[] Ler(int quantidade);
}

/// <summary>
/// Sensor simulado com sinal sintético: ritmos teta, alfa, SMR e beta com amplitudes que variam
/// devagar, ruído da rede em 60 Hz e ruído branco. A variação lenta alterna fases de mais teta
/// e de mais beta, para o índice de engajamento subir e descer durante a sessão. Determinístico
/// para uma mesma semente, o que permite usar a saída como entrada de referência (oráculo).
/// </summary>
public sealed class SimuladorSensor : IFonteSinal
{
    private const double PeriodoModulacao = 40.0;

    private readonly Random _aleatorio;
    private long _indice;

    public SimuladorSensor(double taxaAmostragem = 250, int semente = 42)
    {
        TaxaAmostragem = taxaAmostragem;
        _aleatorio = new Random(semente);
    }

    public double TaxaAmostragem { get; }

    public double[] Ler(int quantidade)
    {
        var amostras = new double[quantidade];
        for (var i = 0; i < quantidade; i++, _indice++)
        {
            var t = _indice / TaxaAmostragem;
            var fase = 0.5 * (1 + Math.Sin(2 * Math.PI * t / PeriodoModulacao));
            var teta = (14 - 8 * fase) * Math.Sin(2 * Math.PI * 6 * t);
            var alfa = 10 * Math.Sin(2 * Math.PI * 10 * t + 0.3);
            var smr = (3 + 3 * fase) * Math.Sin(2 * Math.PI * 13.5 * t + 1.1);
            var beta = (3 + 6 * fase) * Math.Sin(2 * Math.PI * 20 * t + 2.0);
            var rede = 4 * Math.Sin(2 * Math.PI * 60 * t);
            var ruido = 3 * (2 * _aleatorio.NextDouble() - 1);
            amostras[i] = teta + alfa + smr + beta + rede + ruido;
        }

        return amostras;
    }
}
