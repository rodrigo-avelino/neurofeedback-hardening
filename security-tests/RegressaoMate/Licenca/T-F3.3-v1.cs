using System.IO;
using System.Text.Json.Nodes;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Folha F3.3 na forma da v1. O T-F3.3 do red edita uma licença no formato da v0, que a v1
/// recusa só pelo formato. Este teste edita em claro uma licença legítima da v1: parte da
/// licença da "Clínica de Teste" (emitida para outra máquina), troca o código da máquina pelo
/// desta e estica a validade. Com a máquina certa, o único que pode barrar é a assinatura, que
/// cobre os dois campos. Escrito pelo blue a partir da issue da validade editada (#10), depois da
/// tentativa em caixa preta.
/// </summary>
public sealed class T_F3_3_CampoEditado
{
    [Fact(DisplayName = "T-F3.3 (v1) Editar em claro uma licença legítima: máquina e validade trocadas")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();
        var maquina = copia.ObterCodigoDaMaquina();

        copia.InstalarLicencaDeTeste();
        var caminho = copia.Caminho(Alvo.ArquivoLicenca);
        var licenca = JsonNode.Parse(File.ReadAllText(caminho))!.AsObject();
        licenca["maquina"] = maquina;
        licenca["validade"] = "2100-12-31";
        File.WriteAllText(caminho, licenca.ToJsonString());

        var resultado = copia.Executar();
        EstadoFolhas.Verificar("F3.3", ataqueFuncionou: resultado.JanelaPrincipalAbriu);
    }
}
