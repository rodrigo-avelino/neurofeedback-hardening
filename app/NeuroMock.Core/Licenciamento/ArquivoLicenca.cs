using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NeuroMock.Core.Licenciamento;

/// <summary>
/// Os dois arquivos da ativação, ao lado do executável: a licença que o fornecedor envia e a
/// solicitação que o app grava com o código da máquina para o cliente mandar ao fornecedor.
/// </summary>
public static class ArquivoLicenca
{
    public const string NomeLicenca = "licenca.json";
    public const string NomeSolicitacao = "solicitacao-ativacao.json";

    /// <summary>Lê a licença; ausente ou malformada devolve <c>null</c>.</summary>
    public static Licenca? Ler(string pastaAplicativo) =>
        LerJson(Path.Combine(pastaAplicativo, NomeLicenca), ContextoJsonLicenca.Default.Licenca);

    public static string Serializar(Licenca licenca) =>
        JsonSerializer.Serialize(licenca, ContextoJsonLicenca.Default.Licenca);

    public static SolicitacaoAtivacao? LerSolicitacao(string caminhoArquivo) =>
        LerJson(caminhoArquivo, ContextoJsonLicenca.Default.SolicitacaoAtivacao);

    /// <summary>Grava a solicitação de ativação; devolve o caminho, ou <c>null</c> se a pasta não aceita escrita.</summary>
    public static string? GravarSolicitacao(string pastaAplicativo, string maquina)
    {
        var caminho = Path.Combine(pastaAplicativo, NomeSolicitacao);
        try
        {
            File.WriteAllText(caminho, JsonSerializer.Serialize(
                new SolicitacaoAtivacao(maquina), ContextoJsonLicenca.Default.SolicitacaoAtivacao));
            return caminho;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static T? LerJson<T>(string caminho, JsonTypeInfo<T> tipo) where T : class
    {
        if (!File.Exists(caminho))
            return null;

        try
        {
            return JsonSerializer.Deserialize(File.ReadAllText(caminho), tipo);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
