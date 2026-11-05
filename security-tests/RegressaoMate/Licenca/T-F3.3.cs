using Xunit;

namespace RegressaoMate;

/// <summary>
/// Folha F3.3 - Editar o licenca.json em claro (validade à vontade).
/// A licença da v0 é JSON em texto puro e sem assinatura. Em caixa preta descobrimos que a chave
/// não é um simples carimbo de formato: trocar o nome do cliente mantendo a chave faz o app
/// recusar, então a chave valida o cliente (forjar uma chave para outro cliente é a folha F3.2,
/// keygen). A validade, porém, fica fora da chave: dá para abrir o licenca.json num editor e
/// esticar a data de expiração à vontade, mantendo o par cliente/chave legítimo, que o app
/// aceita assim mesmo. É o buraco de autenticidade (CWE-345): a parte validada não cobre a
/// validade, então um pirata renova a licença para sempre sem precisar de assinatura nova.
/// Diferente da F3.1 (reusar a licença verbatim), aqui o arquivo é editado.
/// </summary>
public sealed class T_F3_3
{
    [Fact(DisplayName = "T-F3.3 Editar o licenca.json em claro (validade à vontade)")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();
        copia.RemoverLicenca();   // o pirata parte da licença legítima e só reescreve a data

        // par cliente/chave legítimo (o mesmo do controle positivo), com a validade esticada à
        // mão: a data fica fora da chave, então o app aceita a licença editada
        const string jsonForjado =
            """
            { "cliente": "Clínica de Teste", "validade": "2042-01-01", "chave": "NM-7744-1E91" }
            """;
        copia.InstalarLicenca(jsonForjado);

        var resultado = copia.Executar();

        // folha declarada "aberta": ataque funcionar = teste passa (buraco documentado)
        EstadoFolhas.Verificar("F3.3", ataqueFuncionou: resultado.JanelaPrincipalAbriu);
    }
}
