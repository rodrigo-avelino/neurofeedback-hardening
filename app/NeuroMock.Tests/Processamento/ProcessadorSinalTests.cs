using System;
using System.Collections.Generic;
using System.Linq;
using NeuroMock.Core.Processamento;
using NeuroMock.Core.Sensor;
using Xunit;

namespace NeuroMock.Tests.Processamento;

public sealed class ProcessadorSinalTests
{
    private const double Taxa = 250;

    [Fact]
    public void ParametrosPadraoCarregamEValidam()
    {
        var parametros = ParametrosProcessamento.CarregarPadrao();
        parametros.Validar();
        Assert.Equal(Taxa, parametros.TaxaAmostragem);
        Assert.Equal(new Banda(8, 12), parametros.BandaAlfa);
    }

    [Fact]
    public void UmResultadoPorPassoDepoisDaPrimeiraJanela()
    {
        var processador = ProcessadorSinal.ComParametrosPadrao();
        // janela de 500 e passo de 125: 1000 amostras fecham as janelas em 500, 625, 750, 875 e 1000
        Assert.Equal(5, processador.Processar(new double[1000]).Count);
    }

    [Fact]
    public void EntregaEmPartesDaOMesmoResultadoQueDeUmaVez()
    {
        var sinal = new SimuladorSensor(semente: 3).Ler(3000);
        var deUmaVez = ProcessadorSinal.ComParametrosPadrao().Processar(sinal);

        var emPartes = new List<ResultadoJanela>();
        var processador = ProcessadorSinal.ComParametrosPadrao();
        for (var inicio = 0; inicio < sinal.Length; inicio += 62)
            emPartes.AddRange(processador.Processar(sinal.AsSpan(inicio, Math.Min(62, sinal.Length - inicio))));

        Assert.Equal(deUmaVez, emPartes);
    }

    [Fact]
    public void SenoEmDezHertzCaiNaBandaAlfa()
    {
        var ultima = ProcessadorSinal.ComParametrosPadrao().Processar(Seno(10, 10, 2000)).Last();
        Assert.True(ultima.Alfa > 10 * Math.Max(ultima.Teta, Math.Max(ultima.Smr, ultima.Beta)));
    }

    [Fact]
    public void BetaDominanteDaIndiceMaiorQueTetaDominante()
    {
        var tetaDominante = Somar(Seno(6, 20, 2500), Seno(20, 3, 2500));
        var betaDominante = Somar(Seno(6, 3, 2500), Seno(20, 20, 2500));

        var indiceTeta = ProcessadorSinal.ComParametrosPadrao().Processar(tetaDominante).Last().Indice;
        var indiceBeta = ProcessadorSinal.ComParametrosPadrao().Processar(betaDominante).Last().Indice;

        Assert.True(indiceBeta > 10 * indiceTeta);
    }

    [Fact]
    public void SemRecompensaAntesDoHistoricoMinimo()
    {
        var parametros = ParametrosProcessamento.CarregarPadrao();
        var resultados = new ProcessadorSinal(parametros).Processar(new SimuladorSensor().Ler(5000));
        Assert.All(resultados.Take(parametros.HistoricoMinimo), r => Assert.False(r.Recompensa));
    }

    [Fact]
    public void RecompensaApareceQuandoOIndiceSobe()
    {
        var processador = ProcessadorSinal.ComParametrosPadrao();
        processador.Processar(Somar(Seno(6, 20, 5000), Seno(20, 3, 5000)));
        var subida = processador.Processar(Somar(Seno(6, 3, 2500), Seno(20, 20, 2500)));
        Assert.Contains(subida, r => r.Recompensa);
    }

    private static double[] Seno(double frequencia, double amplitude, int amostras)
    {
        var sinal = new double[amostras];
        for (var i = 0; i < amostras; i++)
            sinal[i] = amplitude * Math.Sin(2 * Math.PI * frequencia * i / Taxa);
        return sinal;
    }

    private static double[] Somar(double[] a, double[] b) => a.Zip(b, (x, y) => x + y).ToArray();
}
