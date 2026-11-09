using System;
using System.IO;

namespace NeuroMock.Core.Processamento;

/// <summary>Faixa de frequência em Hz, fechada embaixo e aberta em cima.</summary>
public sealed record Banda(double Min, double Max);

/// <summary>
/// Parâmetros do pipeline. Até a v2 vinham de um JSON embutido no assembly, em claro: extrair
/// esses números era abrir um arquivo (folha H1.2 da árvore 2). Na v3 o JSON sai do pacote e os
/// valores são decodificados em runtime de um bloco de bytes mascarado, sem recurso em claro e
/// sem os imediatos numéricos no IL. A máscara não protege nada criptograficamente (os valores
/// existem em memória depois de decodificados, teto honesto do MATE): só tira a configuração de
/// uma leitura trivial e, junto com a renomeação, encarece a descompilação.
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
    public static ParametrosProcessamento CarregarPadrao()
    {
        // A ordem de leitura espelha a de escrita do gerador (BinaryWriter, little-endian). A
        // avaliação dos argumentos é da esquerda para a direita, então cada leitura cai no campo
        // certo.
        using var fluxo = new MemoryStream(Desmascarar());
        using var leitor = new BinaryReader(fluxo);
        return new ParametrosProcessamento(
            leitor.ReadDouble(),                                   // TaxaAmostragem
            leitor.ReadInt32(),                                    // TamanhoJanela
            leitor.ReadInt32(),                                    // Passo
            leitor.ReadInt32(),                                    // TamanhoFft
            leitor.ReadDouble(),                                   // FrequenciaRede
            leitor.ReadDouble(),                                   // QualidadeNotch
            leitor.ReadDouble(),                                   // CorteAltaPassagem
            new Banda(leitor.ReadDouble(), leitor.ReadDouble()),   // BandaTeta
            new Banda(leitor.ReadDouble(), leitor.ReadDouble()),   // BandaAlfa
            new Banda(leitor.ReadDouble(), leitor.ReadDouble()),   // BandaSmr
            new Banda(leitor.ReadDouble(), leitor.ReadDouble()),   // BandaBeta
            leitor.ReadDouble(),                                   // PesoTeta
            leitor.ReadDouble(),                                   // PesoAlfa
            leitor.ReadDouble(),                                   // PesoSmr
            leitor.ReadDouble(),                                   // PesoBeta
            leitor.ReadDouble(),                                   // Suavizacao
            leitor.ReadDouble(),                                   // FatorLimiar
            leitor.ReadInt32(),                                    // TamanhoHistorico
            leitor.ReadInt32());                                   // HistoricoMinimo
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

    private static byte[] Desmascarar()
    {
        var saida = new byte[Mascarados.Length];
        for (var i = 0; i < saida.Length; i++)
            saida[i] = (byte)(Mascarados[i] ^ Chave[i % Chave.Length]);
        return saida;
    }

    private static ReadOnlySpan<byte> Mascarados =>
    [
        0x3A, 0x9F, 0xC1, 0x07, 0x5E, 0xF8, 0x42, 0x24, 0x05, 0x89, 0x0C, 0xA3, 0x3A, 0xD2, 0x6B, 0x19,
        0x3A, 0x9D, 0xC1, 0x07, 0x5E, 0xB8, 0x2D, 0x64, 0xF1, 0x88, 0x42, 0xE3, 0x47, 0xD2, 0x6B, 0x19,
        0x3A, 0x9F, 0xFF, 0x47, 0x5E, 0xB8, 0x2D, 0x64, 0xF1, 0x88, 0xFC, 0x9C, 0x47, 0xD2, 0x6B, 0x19,
        0x3A, 0x9F, 0xD1, 0x47, 0x5E, 0xB8, 0x2D, 0x64, 0xF1, 0x88, 0x2C, 0xE3, 0x47, 0xD2, 0x6B, 0x19,
        0x3A, 0x9F, 0xE1, 0x47, 0x5E, 0xB8, 0x2D, 0x64, 0xF1, 0x88, 0x24, 0xE3, 0x47, 0xD2, 0x6B, 0x19,
        0x3A, 0x9F, 0xE9, 0x47, 0x5E, 0xB8, 0x2D, 0x64, 0xF1, 0x88, 0x22, 0xE3, 0x47, 0xD2, 0x6B, 0x19,
        0x3A, 0x9F, 0xEF, 0x47, 0x5E, 0xB8, 0x2D, 0x64, 0xF1, 0x88, 0x32, 0xE3, 0x47, 0xD2, 0x6B, 0x19,
        0x3A, 0x9F, 0x31, 0x38, 0xC4, 0x21, 0xB4, 0xFD, 0x68, 0x11, 0xE5, 0x9C, 0x74, 0xE1, 0x58, 0x2A,
        0x09, 0xAC, 0x22, 0x38, 0x6D, 0x8B, 0x1E, 0x57, 0xC2, 0xBB, 0xFF, 0x9C, 0xDD, 0x4B, 0xF2, 0x80,
        0xA3, 0x06, 0x08, 0x38, 0x93, 0x74, 0xE1, 0xA8, 0x3D, 0x44, 0xFC, 0x9C, 0x7B, 0xD2, 0x6B, 0x19,
        0x32, 0x9F, 0xC1, 0x07,
    ];

    private static ReadOnlySpan<byte> Chave =>
    [
        0x3A, 0x9F, 0xC1, 0x07, 0x5E, 0xB8, 0x2D, 0x64, 0xF1, 0x88, 0x0C, 0xA3, 0x47, 0xD2, 0x6B, 0x19,
    ];
}
