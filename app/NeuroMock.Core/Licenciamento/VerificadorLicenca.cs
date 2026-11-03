using System;
using System.IO;
using System.Text.Json;

namespace NeuroMock.Core.Licenciamento;

/// <summary>
/// Verificação de licença da build 0: lê <c>licenca.json</c> ao lado do executável e devolve um
/// booleano. A decisão inteira fica num único ponto do cliente, sem integridade do binário e
/// sem amarração à máquina. É o baseline que o red team ataca na árvore 1.
/// </summary>
public static class VerificadorLicenca
{
    public const string NomeArquivo = "licenca.json";

    public static bool LicencaValida(string pastaAplicativo) =>
        LicencaValida(pastaAplicativo, DateOnly.FromDateTime(DateTime.Today));

    public static bool LicencaValida(string pastaAplicativo, DateOnly hoje)
    {
        var licenca = Ler(pastaAplicativo);
        return licenca is not null
            && licenca.Validade >= hoje
            && licenca.Chave == GeradorChave.Gerar(licenca.Cliente);
    }

    /// <summary>Lê o arquivo de licença; ausente ou malformado devolve <c>null</c>.</summary>
    public static Licenca? Ler(string pastaAplicativo)
    {
        var caminho = Path.Combine(pastaAplicativo, NomeArquivo);
        if (!File.Exists(caminho))
            return null;

        try
        {
            return JsonSerializer.Deserialize(File.ReadAllText(caminho), ContextoJsonLicenca.Default.Licenca);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
