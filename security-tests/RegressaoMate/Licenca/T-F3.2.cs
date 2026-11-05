using System.Text;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Folha F3.2 - Keygen: reverter a validação da chave e gerar uma válida.
/// A chave da licença é só um hash FNV-1a público do nome do cliente (Trim + maiúsculas),
/// formatado como NM-XXXX-XXXX onde XXXXXXXX é o hash em hexadecimal. Não há segredo: quem
/// descompila NeuroMock.Core.dll lê GeradorChave.Gerar e VerificadorLicenca (que recalcula a
/// chave e compara), reimplementa o algoritmo e emite uma chave válida para qualquer cliente.
/// Aqui o atacante forja uma licença para um cliente que o fornecedor nunca emitiu e o app
/// aceita. Buraco de autenticidade (CWE-345/CWE-321): validação por hash reproduzível, sem
/// assinatura. A correção da v1 (ECDSA) troca o hash por uma assinatura que só a chave privada
/// gera. O keygen é reimplementado AQUI de propósito: o teste observa o app de fora, sem chamar
/// o código do alvo.
/// </summary>
public sealed class T_F3_2
{
    [Fact(DisplayName = "T-F3.2 Keygen: forjar uma chave válida para um cliente qualquer")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();
        copia.RemoverLicenca();   // o pirata não tem licença; vai emitir a dele com o keygen

        // cliente que o fornecedor nunca emitiu + chave gerada pelo keygen reimplementado
        const string cliente = "Pirata Cursos Online";
        var chave = GerarChave(cliente);
        var licencaForjada =
            $$"""
            { "cliente": "{{cliente}}", "validade": "2042-01-01", "chave": "{{chave}}" }
            """;
        copia.InstalarLicenca(licencaForjada);

        var resultado = copia.Executar();

        // folha declarada "aberta": ataque funcionar = teste passa (buraco documentado)
        EstadoFolhas.Verificar("F3.2", ataqueFuncionou: resultado.JanelaPrincipalAbriu);
    }

    /// <summary>
    /// Keygen: o mesmo FNV-1a de 32 bits que o GeradorChave do alvo usa (semente 2166136261,
    /// primo 16777619), reimplementado pelo atacante a partir do que leu no ILSpy. Reproduz a
    /// chave de qualquer cliente sem nenhum segredo. Oráculo: "Clínica de Teste" -> NM-7744-1E91.
    /// </summary>
    private static string GerarChave(string cliente)
    {
        uint num = 2166136261u;
        foreach (var b in Encoding.UTF8.GetBytes(cliente.Trim().ToUpperInvariant()))
        {
            num ^= b;
            num *= 16777619u;
        }

        var hex = num.ToString("X8");
        return "NM-" + hex.Substring(0, 4) + "-" + hex.Substring(4);
    }
}
