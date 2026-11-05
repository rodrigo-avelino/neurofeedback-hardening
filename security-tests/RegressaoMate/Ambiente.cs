using System;
using Xunit;

// Um alvo por vez: cada teste abre o aplicativo de verdade, e janelas em paralelo confundiriam a
// observação e disputariam a máquina.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace RegressaoMate;

/// <summary>Configuração que o run-all.ps1 passa por variável de ambiente.</summary>
internal static class Ambiente
{
    public static string Artefato => Obrigatoria("NFH_ARTEFATO");

    public static string ArquivoEstado => Obrigatoria("NFH_ESTADO");

    public static string Fixtures => Obrigatoria("NFH_FIXTURES");

    public static string PastaTrabalho => Obrigatoria("NFH_TRABALHO");

    public static string? ArquivoResultados => Environment.GetEnvironmentVariable("NFH_RESULTADOS");

    /// <summary>Chave privada do fornecedor em PEM: secret da esteira, ou -ChavePrivada no run-all.ps1 do blue.</summary>
    public static string? ChavePrivadaFornecedor =>
        Environment.GetEnvironmentVariable("NFH_CHAVE_PRIVADA") is { Length: > 0 } pem ? pem : null;

    public static bool NaEsteira => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

    private static string Obrigatoria(string nome) =>
        Environment.GetEnvironmentVariable(nome) is { Length: > 0 } valor
            ? valor
            : throw new InvalidOperationException(
                $"{nome} não definida: rode a suíte por security-tests/run-all.ps1 -Artefato <zip da release>");
}
