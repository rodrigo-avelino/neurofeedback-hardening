using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuroMock.Core.Processamento;

/// <summary>Resultado de uma janela analisada. Potências em unidades arbitrárias.</summary>
public sealed record ResultadoJanela(
    double Teta,
    double Alfa,
    double Smr,
    double Beta,
    double Indice,
    double IndiceSuavizado,
    double Limiar,
    bool Recompensa);

/// <summary>
/// Stub do núcleo de processamento (o DSP real não entra neste repo). Por amostra: passa-altas
/// e notch da rede. A cada passo: janela de Hann, FFT, potência por banda, índice de engajamento
/// ponderado, suavização exponencial e limiar adaptativo pela mediana do histórico. É um
/// algoritmo de verdade, com estado e parâmetros, para que a árvore 2 meça algo que transfira
/// para o produto real.
/// </summary>
public sealed class ProcessadorSinal
{
    private const double Epsilon = 1e-12;

    private readonly ParametrosProcessamento _parametros;
    private readonly FiltroBiquad _passaAltas;
    private readonly FiltroBiquad _notch;
    private readonly double[] _hann;
    private readonly List<double> _filtradas;
    private readonly Queue<double> _historico = new();
    private double? _indiceSuavizado;

    public ProcessadorSinal(ParametrosProcessamento parametros)
    {
        parametros.Validar();
        _parametros = parametros;
        _passaAltas = FiltroBiquad.PassaAltas(parametros.TaxaAmostragem, parametros.CorteAltaPassagem);
        _notch = FiltroBiquad.Rejeita(parametros.TaxaAmostragem, parametros.FrequenciaRede, parametros.QualidadeNotch);
        _hann = JanelaHann(parametros.TamanhoJanela);
        _filtradas = new List<double>(parametros.TamanhoJanela);
    }

    public static ProcessadorSinal ComParametrosPadrao() => new(ParametrosProcessamento.CarregarPadrao());

    /// <summary>Filtra as amostras novas e devolve um resultado para cada janela completada.</summary>
    public IReadOnlyList<ResultadoJanela> Processar(ReadOnlySpan<double> amostras)
    {
        var resultados = new List<ResultadoJanela>();
        foreach (var amostra in amostras)
        {
            _filtradas.Add(_notch.Processar(_passaAltas.Processar(amostra)));
            if (_filtradas.Count == _parametros.TamanhoJanela)
            {
                resultados.Add(AnalisarJanela());
                _filtradas.RemoveRange(0, _parametros.Passo);
            }
        }

        return resultados;
    }

    private ResultadoJanela AnalisarJanela()
    {
        var p = _parametros;
        var re = new double[p.TamanhoFft];
        var im = new double[p.TamanhoFft];
        var media = _filtradas.Average();
        for (var i = 0; i < p.TamanhoJanela; i++)
            re[i] = (_filtradas[i] - media) * _hann[i];
        Fft.Transformar(re, im);

        var resolucao = p.TaxaAmostragem / p.TamanhoFft;
        double Potencia(Banda banda)
        {
            var soma = 0.0;
            for (var k = 1; k <= p.TamanhoFft / 2; k++)
            {
                var frequencia = k * resolucao;
                if (frequencia >= banda.Min && frequencia < banda.Max)
                    soma += re[k] * re[k] + im[k] * im[k];
            }

            return soma;
        }

        var teta = Potencia(p.BandaTeta);
        var alfa = Potencia(p.BandaAlfa);
        var smr = Potencia(p.BandaSmr);
        var beta = Potencia(p.BandaBeta);
        var indice = (p.PesoBeta * beta + p.PesoSmr * smr) / (p.PesoTeta * teta + p.PesoAlfa * alfa + Epsilon);

        var suavizado = _indiceSuavizado is { } anterior
            ? p.Suavizacao * indice + (1 - p.Suavizacao) * anterior
            : indice;
        _indiceSuavizado = suavizado;

        // Sem histórico, o limiar fica acima do índice atual: nada de recompensa no começo.
        var limiar = (_historico.Count > 0 ? Mediana(_historico) : suavizado) * p.FatorLimiar;
        var recompensa = _historico.Count >= p.HistoricoMinimo && suavizado > limiar;

        _historico.Enqueue(suavizado);
        if (_historico.Count > p.TamanhoHistorico)
            _historico.Dequeue();

        return new ResultadoJanela(teta, alfa, smr, beta, indice, suavizado, limiar, recompensa);
    }

    private static double[] JanelaHann(int tamanho)
    {
        var janela = new double[tamanho];
        for (var i = 0; i < tamanho; i++)
            janela[i] = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (tamanho - 1)));
        return janela;
    }

    private static double Mediana(IEnumerable<double> valores)
    {
        var ordenados = valores.OrderBy(v => v).ToArray();
        var meio = ordenados.Length / 2;
        return ordenados.Length % 2 == 1 ? ordenados[meio] : (ordenados[meio - 1] + ordenados[meio]) / 2;
    }
}
