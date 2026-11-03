using System;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Antes de qualquer ataque: o alvo original abre o treino com licença válida e recusa sem
/// licença. Se isso não vale, nenhum resultado de ataque significa nada (um build quebrado que
/// nem abre pareceria seguro), então todo teste de folha vira inconclusivo.
/// </summary>
public static class ControlePositivo
{
    private static readonly Lazy<string?> Falha = new(Avaliar);

    /// <summary>Chamar no começo de todo teste de folha.</summary>
    public static void Garantir()
    {
        if (Falha.Value is { } motivo)
            Assert.Fail($"INCONCLUSIVO: o controle positivo falhou: {motivo}");
    }

    private static string? Avaliar()
    {
        var motivo = Observar(copia => copia.InstalarLicencaDeTeste(), r => r.JanelaPrincipalAbriu,
                "com licença válida o treino não abriu")
            ?? Observar(copia => copia.RemoverLicenca(), r => r.Recusou,
                "sem licença o alvo não recusou de forma observável");

        Resultados.RegistrarControle(motivo);
        return motivo;
    }

    private static string? Observar(Action<CopiaDoAlvo> preparar, Func<ResultadoExecucao, bool> esperado, string falha)
    {
        using var copia = Alvo.NovaCopia();
        preparar(copia);
        var resultado = copia.Executar();
        return esperado(resultado) ? null : $"{falha} ({resultado})";
    }
}

public sealed class ControlePositivoTestes
{
    [Fact(DisplayName = "Controle positivo: abre com licença válida e recusa sem licença")]
    public void AlvoOriginalSeComportaComoEsperado() => ControlePositivo.Garantir();
}
