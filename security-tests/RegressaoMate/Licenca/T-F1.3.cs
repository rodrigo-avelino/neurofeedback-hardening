using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Folha F1.3 - Trocar o verificador de licença por um stub que sempre libera.
/// A v1 assina a licença (ECDSA) e prende ao hardware, então forjar licença (G3) morreu. Mas a
/// validação inteira desemboca num bool: VerificadorLicenca.LicencaValida(string) decide se o
/// treino abre. O atacante não quebra a assinatura; patcha o NeuroMock.Core.dll e troca o corpo
/// desse método por "return true", com dnlib (a mesma lib do dnSpy). Sem licença nenhuma, o app
/// abre. É o argumento do G1: a raiz é a integridade do binário; por isso a v2 vai assinar um
/// manifesto de hashes. Funciona na v1 porque ela ainda não confere o próprio binário.
/// </summary>
public sealed class T_F1_3
{
    [Fact(DisplayName = "T-F1.3 Trocar o verificador de licença por um stub que sempre libera")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();
        if (copia.EhNativo)
        {
            // v4 Native AOT: nao ha NeuroMock.Core.dll para trocar o corpo do verificador por um
            // stub. Ataque neutralizado por construcao.
            EstadoFolhas.Verificar("F1.3", ataqueFuncionou: false);
            return;
        }

        copia.RemoverLicenca();   // o pirata não tem licença; o stub torna isso irrelevante

        // Patch estático no NeuroMock.Core.dll: troca o corpo de
        // VerificadorLicenca.LicencaValida(string) por "return true".
        Assembleia.Modificar(copia.Caminho("NeuroMock.Core.dll"), modulo =>
        {
            var tipo = modulo.Find("NeuroMock.Core.Licenciamento.VerificadorLicenca", isReflectionName: true)
                ?? throw new InvalidOperationException("tipo VerificadorLicenca não encontrado");

            // o método que o app chama para decidir a tela: bool LicencaValida(string)
            var metodo = tipo.FindMethod("LicencaValida",
                MethodSig.CreateStatic(modulo.CorLibTypes.Boolean, modulo.CorLibTypes.String))
                ?? throw new InvalidOperationException("método LicencaValida(string) não encontrado");

            var corpo = new CilBody();
            corpo.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());  // empilha 1 (true)
            corpo.Instructions.Add(OpCodes.Ret.ToInstruction());       // retorna
            corpo.MaxStack = 1;
            metodo.Body = corpo;   // o corpo original (máquina, data, assinatura) é descartado
        });

        var resultado = copia.Executar();

        // folha declarada "aberta": ataque funcionar = teste passa (buraco documentado)
        EstadoFolhas.Verificar("F1.3", ataqueFuncionou: resultado.JanelaPrincipalAbriu);
    }
}