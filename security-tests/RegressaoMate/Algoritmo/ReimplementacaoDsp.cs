using System;
using System.Collections.Generic;
using System.Linq;

// Reimplementacao do pipeline de DSP do NeuroMock (folha H2.2), reconstruida em caixa preta a
// partir das classes descompiladas (FiltroBiquad, Fft, ProcessadorSinal, ParametrosProcessamento).
// Fica em namespace proprio para nao colidir com o original, que o teste carrega por reflexao.
namespace RegressaoMate.ReimplH22;

public readonly struct Banda
{
    public double Min { get; }
    public double Max { get; }
    public Banda(double min, double max) { Min = min; Max = max; }
}

public readonly struct ResultadoJanela
{
    public double PotenciaTeta { get; }
    public double PotenciaAlfa { get; }
    public double PotenciaSmr { get; }
    public double PotenciaBeta { get; }
    public double Indice { get; }
    public double IndiceSuavizado { get; }
    public double Limiar { get; }
    public bool Recompensa { get; }

    public ResultadoJanela(double teta, double alfa, double smr, double beta,
        double indice, double indiceSuavizado, double limiar, bool recompensa)
    {
        PotenciaTeta = teta; PotenciaAlfa = alfa; PotenciaSmr = smr; PotenciaBeta = beta;
        Indice = indice; IndiceSuavizado = indiceSuavizado; Limiar = limiar; Recompensa = recompensa;
    }
}

public sealed class ParametrosProcessamento
{
    public double TaxaAmostragem;
    public int TamanhoJanela;
    public int Passo;
    public int TamanhoFft;
    public double FrequenciaRede;
    public double QualidadeNotch;
    public double CorteAltaPassagem;
    public Banda BandaTeta;
    public Banda BandaAlfa;
    public Banda BandaSmr;
    public Banda BandaBeta;
    public double PesoTeta;
    public double PesoAlfa;
    public double PesoSmr;
    public double PesoBeta;
    public double Suavizacao;
    public double FatorLimiar;
    public int TamanhoHistorico;
    public int HistoricoMinimo;

    // Valores identicos ao recurso NeuroMock.Core.parametros.json.
    public static ParametrosProcessamento CarregarPadrao() => new()
    {
        TaxaAmostragem = 250.0,
        TamanhoJanela = 500,
        Passo = 125,
        TamanhoFft = 512,
        FrequenciaRede = 60.0,
        QualidadeNotch = 30.0,
        CorteAltaPassagem = 1.0,
        BandaTeta = new Banda(4.0, 8.0),
        BandaAlfa = new Banda(8.0, 12.0),
        BandaSmr = new Banda(12.0, 15.0),
        BandaBeta = new Banda(15.0, 30.0),
        PesoTeta = 1.0,
        PesoAlfa = 0.8,
        PesoSmr = 0.6,
        PesoBeta = 1.2,
        Suavizacao = 0.2,
        FatorLimiar = 1.05,
        TamanhoHistorico = 60,
        HistoricoMinimo = 8,
    };
}

public sealed class FiltroBiquad
{
    private readonly double _b0, _b1, _b2, _a1, _a2;
    private double _x1, _x2, _y1, _y2;

    private FiltroBiquad(double b0, double b1, double b2, double a0, double a1, double a2)
    {
        _b0 = b0 / a0; _b1 = b1 / a0; _b2 = b2 / a0;
        _a1 = a1 / a0; _a2 = a2 / a0;
    }

    public static FiltroBiquad PassaAltas(double taxaAmostragem, double corte, double q = 0.7071067811865476)
    {
        var (cos, alfa) = Prototipo(taxaAmostragem, corte, q);
        return new FiltroBiquad((1.0 + cos) / 2.0, -(1.0 + cos), (1.0 + cos) / 2.0,
                                1.0 + alfa, -2.0 * cos, 1.0 - alfa);
    }

    public static FiltroBiquad Rejeita(double taxaAmostragem, double frequencia, double q)
    {
        var (cos, alfa) = Prototipo(taxaAmostragem, frequencia, q);
        return new FiltroBiquad(1.0, -2.0 * cos, 1.0,
                                1.0 + alfa, -2.0 * cos, 1.0 - alfa);
    }

    public double Processar(double x)
    {
        double y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
        _x2 = _x1; _x1 = x;
        _y2 = _y1; _y1 = y;
        return y;
    }

    private static (double Cos, double Alfa) Prototipo(double taxaAmostragem, double frequencia, double q)
    {
        double w = 2.0 * Math.PI * frequencia / taxaAmostragem;
        return (Math.Cos(w), Math.Sin(w) / (2.0 * q));
    }
}

public static class Fft
{
    public static void Transformar(double[] re, double[] im)
    {
        int n = re.Length;
        if (n == 0 || n != im.Length || (n & (n - 1)) != 0)
            throw new ArgumentException("os vetores precisam ter o mesmo tamanho, potencia de 2", nameof(re));

        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j ^= bit;
            if (i < j)
            {
                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            double ang = -2.0 * Math.PI / len;
            double wRe = Math.Cos(ang);
            double wIm = Math.Sin(ang);
            int meia = len / 2;
            for (int inicio = 0; inicio < n; inicio += len)
            {
                double cRe = 1.0, cIm = 0.0;
                for (int k = 0; k < meia; k++)
                {
                    int a = inicio + k;
                    int b = a + meia;
                    double tRe = re[b] * cRe - im[b] * cIm;
                    double tIm = re[b] * cIm + im[b] * cRe;
                    re[b] = re[a] - tRe;
                    im[b] = im[a] - tIm;
                    re[a] += tRe;
                    im[a] += tIm;
                    double novoRe = cRe * wRe - cIm * wIm;
                    cIm = cRe * wIm + cIm * wRe;
                    cRe = novoRe;
                }
            }
        }
    }
}

public sealed class ProcessadorSinal
{
    private const double Epsilon = 1e-12;

    private readonly ParametrosProcessamento _p;
    private readonly FiltroBiquad _passaAltas;
    private readonly FiltroBiquad _notch;
    private readonly double[] _hann;
    private readonly List<double> _filtradas;
    private readonly Queue<double> _historico = new();
    private double? _indiceSuavizado;

    public ProcessadorSinal(ParametrosProcessamento parametros)
    {
        _p = parametros;
        _passaAltas = FiltroBiquad.PassaAltas(_p.TaxaAmostragem, _p.CorteAltaPassagem, 0.7071067811865476);
        _notch = FiltroBiquad.Rejeita(_p.TaxaAmostragem, _p.FrequenciaRede, _p.QualidadeNotch);
        _hann = JanelaHann(_p.TamanhoJanela);
        _filtradas = new List<double>(_p.TamanhoJanela);
    }

    public static ProcessadorSinal ComParametrosPadrao()
        => new(ParametrosProcessamento.CarregarPadrao());

    public IReadOnlyList<ResultadoJanela> Processar(IReadOnlyList<double> amostras)
    {
        var saida = new List<ResultadoJanela>();
        for (int i = 0; i < amostras.Count; i++)
        {
            double x = amostras[i];
            _filtradas.Add(_notch.Processar(_passaAltas.Processar(x)));
            if (_filtradas.Count == _p.TamanhoJanela)
            {
                saida.Add(AnalisarJanela());
                _filtradas.RemoveRange(0, _p.Passo);
            }
        }
        return saida;
    }

    private ResultadoJanela AnalisarJanela()
    {
        var re = new double[_p.TamanhoFft];
        var im = new double[_p.TamanhoFft];
        double media = _filtradas.Average();
        for (int i = 0; i < _p.TamanhoJanela; i++)
            re[i] = (_filtradas[i] - media) * _hann[i];

        Fft.Transformar(re, im);
        double resolucao = _p.TaxaAmostragem / _p.TamanhoFft;

        double teta = Potencia(_p.BandaTeta, re, im, resolucao);
        double alfa = Potencia(_p.BandaAlfa, re, im, resolucao);
        double smr = Potencia(_p.BandaSmr, re, im, resolucao);
        double beta = Potencia(_p.BandaBeta, re, im, resolucao);

        double indice = (_p.PesoBeta * beta + _p.PesoSmr * smr)
                      / (_p.PesoTeta * teta + _p.PesoAlfa * alfa + Epsilon);

        double suavizado = _indiceSuavizado.HasValue
            ? _p.Suavizacao * indice + (1.0 - _p.Suavizacao) * _indiceSuavizado.Value
            : indice;
        _indiceSuavizado = suavizado;

        double limiar = (_historico.Count > 0 ? Mediana(_historico) : suavizado) * _p.FatorLimiar;
        bool recompensa = _historico.Count >= _p.HistoricoMinimo && suavizado > limiar;

        _historico.Enqueue(suavizado);
        if (_historico.Count > _p.TamanhoHistorico)
            _historico.Dequeue();

        return new ResultadoJanela(teta, alfa, smr, beta, indice, suavizado, limiar, recompensa);
    }

    private double Potencia(Banda banda, double[] re, double[] im, double resolucao)
    {
        double soma = 0.0;
        for (int i = 1; i <= _p.TamanhoFft / 2; i++)
        {
            double freq = i * resolucao;
            if (freq >= banda.Min && freq < banda.Max)
                soma += re[i] * re[i] + im[i] * im[i];
        }
        return soma;
    }

    private static double[] JanelaHann(int tamanho)
    {
        var a = new double[tamanho];
        for (int i = 0; i < tamanho; i++)
            a[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (tamanho - 1)));
        return a;
    }

    private static double Mediana(IEnumerable<double> valores)
    {
        double[] a = valores.OrderBy(v => v).ToArray();
        int meio = a.Length / 2;
        if (a.Length % 2 != 1)
            return (a[meio - 1] + a[meio]) / 2.0;
        return a[meio];
    }
}
