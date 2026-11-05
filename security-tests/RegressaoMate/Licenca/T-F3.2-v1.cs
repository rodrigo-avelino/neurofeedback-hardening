using System;
using NeuroMock.Fornecedor;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Folha F3.2 na forma da v1. O keygen da v0 (T-F3.2, do red) monta uma licença no formato
/// antigo, que a v1 recusa só pelo formato. Este teste ataca o controle da v1: o pirata gera o
/// próprio par de chaves e assina uma licença para esta máquina. Sem a chave privada do
/// fornecedor, a assinatura não confere com a chave pública embutida e o app tem de recusar.
/// Escrito pelo blue a partir da issue do keygen (#12), depois da tentativa em caixa preta.
/// </summary>
public sealed class T_F3_2_AssinaturaDeOutraChave
{
    [Fact(DisplayName = "T-F3.2 (v1) Keygen com a chave do atacante: licença desta máquina assinada por outra chave")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();
        var maquina = copia.ObterCodigoDaMaquina();

        using var chaveDoAtacante = ChavesFornecedor.Gerar();
        var validade = DateOnly.FromDateTime(DateTime.Today).AddYears(1);
        copia.InstalarLicenca(EmissorLicenca.Emitir(chaveDoAtacante, "Pirata Cursos Online", validade, maquina));

        var resultado = copia.Executar();
        EstadoFolhas.Verificar("F3.2", ataqueFuncionou: resultado.JanelaPrincipalAbriu);
    }
}
