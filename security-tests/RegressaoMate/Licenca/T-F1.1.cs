using System;
using System.IO;
using System.Text.Json.Nodes;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Folha F1.1 - Editar o IL da validação e salvar o assembly. Diferente do F1.3 (stub que pula a
/// verificação inteira) e do F1.2 (NOP no salto do portão no App), aqui a verificação continua
/// rodando. O teste faz a MESMA adulteração em claro do T-F3.3 (v1): parte da licença legítima da
/// "Clínica de Teste" (emitida para outra máquina), troca o código da máquina pelo desta e estica
/// a validade. Isso a v1 recusa só pela assinatura ECDSA, que cobre cliente+validade+maquina. O
/// que o F1.1 acrescenta é editar o IL de VerificadorLicenca.AssinaturaConfere: logo depois do
/// VerifyData insere "pop; ldc.i4.1", descartando o veredito real do ECDSA e devolvendo true. O
/// VerifyData ainda roda, mas a resposta dele é ignorada, então a licença adulterada (sem a chave
/// privada do fornecedor) passa. Mostra que a assinatura, defesa principal da v1, cai com um edit
/// cirúrgico de duas instruções porque o binário não confere a própria integridade. Defesa:
/// anti-tamper / checagem de integridade (v2).
/// </summary>
public sealed class T_F1_1
{
    [Fact(DisplayName = "T-F1.1 Editar o IL da validação: AssinaturaConfere aceita qualquer assinatura")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();
        var maquina = copia.ObterCodigoDaMaquina();

        // mesma adulteração em claro do T-F3.3 (v1): máquina desta máquina e validade no futuro.
        // só a assinatura ECDSA barra, e é nela que o edit de IL vai bater.
        copia.InstalarLicencaDeTeste();
        var caminho = copia.Caminho(Alvo.ArquivoLicenca);
        var licenca = JsonNode.Parse(File.ReadAllText(caminho))!.AsObject();
        licenca["maquina"] = maquina;
        licenca["validade"] = "2100-12-31";
        File.WriteAllText(caminho, licenca.ToJsonString());

        // edit cirúrgico no IL: sobrescreve o veredito do ECDSA dentro de AssinaturaConfere
        Assembleia.Modificar(copia.Caminho("NeuroMock.Core.dll"), modulo =>
        {
            var tipo = modulo.Find("NeuroMock.Core.Licenciamento.VerificadorLicenca", isReflectionName: true)
                ?? throw new InvalidOperationException("tipo VerificadorLicenca não encontrado");
            var metodo = tipo.FindMethod("AssinaturaConfere")
                ?? throw new InvalidOperationException("método AssinaturaConfere não encontrado");

            var instrucoes = metodo.Body.Instructions;

            // acha a chamada a VerifyData; o ret logo em seguida devolve o resultado do ECDSA
            int idx = -1;
            for (int k = 0; k < instrucoes.Count; k++)
            {
                if ((instrucoes[k].OpCode == OpCodes.Callvirt || instrucoes[k].OpCode == OpCodes.Call)
                    && instrucoes[k].Operand is IMethod chamado
                    && chamado.Name == "VerifyData")
                {
                    idx = k;
                    break;
                }
            }
            if (idx < 0 || idx + 1 >= instrucoes.Count || instrucoes[idx + 1].OpCode != OpCodes.Ret)
                throw new InvalidOperationException("não achei o ret logo após VerifyData em AssinaturaConfere");

            // descarta o veredito real e devolve true: ... VerifyData; pop; ldc.i4.1; ret
            instrucoes.Insert(idx + 1, OpCodes.Pop.ToInstruction());
            instrucoes.Insert(idx + 2, OpCodes.Ldc_I4_1.ToInstruction());
        });

        var resultado = copia.Executar();

        EstadoFolhas.Verificar("F1.1", ataqueFuncionou: resultado.JanelaPrincipalAbriu);
    }
}
