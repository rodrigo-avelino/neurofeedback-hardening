using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;

namespace NeuroMock.Core.Integridade;

/// <summary>O arquivo do manifesto na raiz do pacote e o hash dos arquivos que ele lista.</summary>
public static class ArquivoManifesto
{
    public const string Nome = "manifesto.json";

    /// <summary>Lê o manifesto; ausente ou malformado devolve <c>null</c>.</summary>
    public static Manifesto? Ler(string pastaPacote)
    {
        var caminho = Path.Combine(pastaPacote, Nome);
        if (!File.Exists(caminho))
            return null;

        try
        {
            return JsonSerializer.Deserialize(ComRetentativa(() => File.ReadAllText(caminho)), ContextoJsonManifesto.Default.Manifesto);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string Serializar(Manifesto manifesto) =>
        JsonSerializer.Serialize(manifesto, ContextoJsonManifesto.Default.Manifesto);

    /// <summary>
    /// Caminho absoluto de uma entrada do manifesto, ou <c>null</c> se ela é inválida ou apontaria
    /// para fora da pasta do pacote.
    /// </summary>
    public static string? CaminhoNoPacote(string pastaPacote, string caminhoRelativo)
    {
        if (!FormatoManifesto.CaminhoValido(caminhoRelativo))
            return null;

        var raiz = Path.TrimEndingDirectorySeparator(Path.GetFullPath(pastaPacote)) + Path.DirectorySeparatorChar;
        var completo = Path.GetFullPath(Path.Combine(raiz, caminhoRelativo.Replace('/', Path.DirectorySeparatorChar)));
        var comparacao = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return completo.StartsWith(raiz, comparacao) ? completo : null;
    }

    /// <summary>SHA-256 do arquivo, em hexadecimal minúsculo.</summary>
    public static string Sha256(string caminhoAbsoluto) => ComRetentativa(() =>
    {
        // Leitura compartilhada: o executável e os assemblies do próprio app estão abertos pelo loader.
        using var fluxo = new FileStream(
            caminhoAbsoluto, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Convert.ToHexStringLower(SHA256.HashData(fluxo));
    });

    // Num pacote recém-criado (deploy novo, ou as cópias que a esteira extrai do zip), o antivírus
    // pode segurar um arquivo por um instante e a leitura falha com IOException. Isso não é
    // adulteração: tenta de novo algumas vezes (backoff curto, menos de 1 s no total) antes de
    // desistir, para a verificação de integridade não confundir uma trava transitória com um
    // pacote adulterado. Uma trava persistente ainda estoura, e o pacote conta como adulterado.
    private static T ComRetentativa<T>(Func<T> ler)
    {
        for (var tentativa = 0; ; tentativa++)
        {
            try
            {
                return ler();
            }
            catch (IOException) when (tentativa < 8)
            {
                Thread.Sleep(50 + tentativa * 25);
            }
        }
    }
}
