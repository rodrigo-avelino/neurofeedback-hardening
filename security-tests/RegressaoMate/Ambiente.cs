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

    private static string Obrigatoria(string nome) =>
        Environment.GetEnvironmentVariable(nome) is { Length: > 0 } valor
            ? valor
            : throw new InvalidOperationException(
                $"{nome} não definida: rode a suíte por security-tests/run-all.ps1 -Artefato <zip da release>");
}
