using System;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Antes de qualquer ataque: o alvo original recusa sem licença e abre o treino com uma licença
/// legítima para a máquina que roda a suíte. Se isso não vale, nenhum resultado de ataque
/// significa nada (um build quebrado que nem abre pareceria seguro), então todo teste de folha
/// vira inconclusivo.
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
        // Sem licença: a recusa tem de ser observável, e o alvo deixa a solicitação de ativação
        // com o código desta máquina, como faria para um cliente de verdade.
        string? maquina = null;
        var motivo = Observar(copia => copia.RemoverLicenca(), r => r.Recusou,
            "sem licença o alvo não recusou de forma observável", copia => maquina = copia.LerCodigoDaMaquina());
        if (motivo is null && maquina is null)
            motivo = "o alvo recusou, mas não gravou a solicitação de ativação com o código da máquina";

        // Com licença emitida pelo fornecedor para esta máquina: o treino abre. Só o blue e a
        // esteira têm a chave privada; sem ela, fora da esteira, o controle fica parcial.
        var parcial = false;
        if (motivo is null)
        {
            if (Ambiente.ChavePrivadaFornecedor is { } chave)
            {
                motivo = Observar(copia => copia.InstalarLicencaParaAMaquina(maquina!, chave), r => r.JanelaPrincipalAbriu,
                    "com licença válida para esta máquina o treino não abriu");
            }
            else if (Ambiente.NaEsteira)
            {
                motivo = "a esteira está sem a chave privada do fornecedor (secret NEUROMOCK_CHAVE_PRIVADA)";
            }
            else
            {
                parcial = true;
            }
        }

        Resultados.RegistrarControle(motivo, parcial);
        return motivo;
    }

    private static string? Observar(
        Action<CopiaDoAlvo> preparar, Func<ResultadoExecucao, bool> esperado, string falha, Action<CopiaDoAlvo>? depois = null)
    {
        using var copia = Alvo.NovaCopia();
        preparar(copia);
        var resultado = copia.Executar();
        depois?.Invoke(copia);
        return esperado(resultado) ? null : $"{falha} ({resultado})";
    }
}

public sealed class ControlePositivoTestes
{
    [Fact(DisplayName = "Controle positivo: recusa sem licença e abre com licença válida para esta máquina")]
    public void AlvoOriginalSeComportaComoEsperado() => ControlePositivo.Garantir();
}
