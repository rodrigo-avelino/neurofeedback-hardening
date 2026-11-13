using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Folha F1.2 - Patch de bytes: NOP no salto condicional do portão de licença.
/// Diferente do F1.3 (que troca o verificador por um stub), aqui o verificador fica intacto:
/// o ataque é no CHAMADOR. Em NeuroMock.App.OnFrameworkInitializationCompleted o app faz
/// if (VerificadorLicenca.LicencaValida(...)) abre a MainWindow, senão abre a JanelaAtivacao.
/// No IL isso é um brfalse logo depois da chamada: licença inválida desvia para o ramo da
/// JanelaAtivacao. O atacante troca esse salto condicional por um pop (descarta o bool da pilha
/// sem desviar), e o fluxo sempre cai na MainWindow, ignorando o resultado da verificação.
/// A v1 assina a licença (ECDSA) mas não confere a própria integridade, então um patch de uma
/// instrução no binário do app anula a decisão. A defesa é anti-tamper / integridade (v2).
/// </summary>
public sealed class T_F1_2
{
    [Fact(DisplayName = "T-F1.2 Patch de bytes: NOP no salto condicional do portão de licença")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();
        if (copia.EhNativo)
        {
            // v4 Native AOT: o NOP no salto do portao nao tem IL gerenciado para patchear.
            // Ataque neutralizado por construcao.
            EstadoFolhas.Verificar("F1.2", ataqueFuncionou: false);
            return;
        }

        copia.RemoverLicenca();   // sem licença: o portão mandaria pra JanelaAtivacao

        Assembleia.Modificar(copia.Caminho("NeuroMock.dll"), modulo =>
        {
            var tipo = modulo.Find("NeuroMock.App", isReflectionName: true)
                ?? throw new InvalidOperationException("tipo NeuroMock.App não encontrado");
            var metodo = tipo.FindMethod("OnFrameworkInitializationCompleted")
                ?? throw new InvalidOperationException("método OnFrameworkInitializationCompleted não encontrado");

            var instrucoes = metodo.Body.Instructions;

            // acha a chamada ao verificador; o salto condicional é a instrução seguinte
            int i = -1;
            for (int k = 0; k < instrucoes.Count; k++)
            {
                if (instrucoes[k].OpCode == OpCodes.Call
                    && instrucoes[k].Operand is IMethod chamado
                    && chamado.Name == "LicencaValida")
                {
                    i = k;
                    break;
                }
            }
            if (i < 0 || i + 1 >= instrucoes.Count)
                throw new InvalidOperationException("chamada a LicencaValida não encontrada");

            var salto = instrucoes[i + 1];
            if (salto.OpCode != OpCodes.Brfalse && salto.OpCode != OpCodes.Brfalse_S)
                throw new InvalidOperationException($"esperava brfalse apos LicencaValida, achei {salto.OpCode}");

            // NOP do salto condicional: vira um pop (descarta o bool, sem desviar) -> segue reto pra MainWindow
            salto.OpCode = OpCodes.Pop;
            salto.Operand = null;
        });

        var resultado = copia.Executar();

        EstadoFolhas.Verificar("F1.2", ataqueFuncionou: resultado.JanelaPrincipalAbriu);
    }
}