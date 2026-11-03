using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeuroMock.Core.Processamento;

/// <summary>Faixa de frequência em Hz, fechada embaixo e aberta em cima.</summary>
public sealed record Banda(double Min, double Max);

/// <summary>
/// Parâmetros do pipeline. Na build 0 vêm de um JSON embutido no assembly, em claro: extrair
/// esses números é a folha H1.2 da árvore 2.
/// </summary>
public sealed record ParametrosProcessamento(
    double TaxaAmostragem,
    int TamanhoJanela,
    int Passo,
    int TamanhoFft,
    double FrequenciaRede,
    double QualidadeNotch,
    double CorteAltaPassagem,
    Banda BandaTeta,
    Banda BandaAlfa,
    Banda BandaSmr,
    Banda BandaBeta,
    double PesoTeta,
    double PesoAlfa,
    double PesoSmr,
    double PesoBeta,
    double Suavizacao,
    double FatorLimiar,
    int TamanhoHistorico,
    int HistoricoMinimo)
{
    private const string Recurso = "NeuroMock.Core.parametros.json";

    public static ParametrosProcessamento CarregarPadrao()
    {
        using var fluxo = typeof(ParametrosProcessamento).Assembly.GetManifestResourceStream(Recurso)
            ?? throw new InvalidOperationException($"recurso {Recurso} ausente do assembly");
        return JsonSerializer.Deserialize(fluxo, ContextoJsonProcessamento.Default.ParametrosProcessamento)
            ?? throw new InvalidOperationException("parâmetros de processamento vazios");
    }

    public void Validar()
    {
        if (TaxaAmostragem <= 0)
            throw new ArgumentException("a taxa de amostragem precisa ser positiva");
        if (TamanhoJanela < 2 || Passo < 1 || Passo > TamanhoJanela)
            throw new ArgumentException("janela e passo inconsistentes");
        if (TamanhoFft < TamanhoJanela || (TamanhoFft & (TamanhoFft - 1)) != 0)
            throw new ArgumentException("o tamanho da FFT precisa ser potência de 2 e caber a janela");
        if (Suavizacao is <= 0 or > 1)
            throw new ArgumentException("a suavização precisa estar em (0, 1]");
        if (HistoricoMinimo < 1 || HistoricoMinimo > TamanhoHistorico)
            throw new ArgumentException("histórico mínimo precisa estar entre 1 e o tamanho do histórico");
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(ParametrosProcessamento))]
internal sealed partial class ContextoJsonProcessamento : JsonSerializerContext
{
}
