using System;
using System.IO;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Folha F2.2 (arvore 1, pirataria): hook/detour do metodo de licenca em runtime.
/// Ataque de caixa preta: injeta o HookLicenca.dll no alvo via DOTNET_STARTUP_HOOKS (que o
/// processo filho herda). O hook usa Harmony para fazer VerificadorLicenca.LicencaValida sempre
/// retornar true, sem alterar nenhum arquivo do pacote (a integridade da v2 nao pega hook em
/// memoria). O ataque "funcionou" se o treino abre sem licenca.
/// </summary>
public sealed class T_F2_2
{
    [Fact(DisplayName = "T-F2.2 Hook/detour do metodo de licenca (Harmony via startup hook)")]
    public void Ataque()
    {
        // Confirma o alvo original ANTES de injetar o hook (o controle nao pode receber o grampo).
        ControlePositivo.Garantir();

        var hook = Path.Combine(AppContext.BaseDirectory, "HookLicenca.dll");
        Assert.True(File.Exists(hook), $"HookLicenca.dll nao encontrado em {hook}");

        var anterior = Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS");
        Environment.SetEnvironmentVariable("DOTNET_STARTUP_HOOKS", hook);
        try
        {
            using var copia = Alvo.NovaCopia();
            copia.RemoverLicenca();                       // o pirata nao tem licenca
            var resultado = copia.Executar();             // o filho herda DOTNET_STARTUP_HOOKS

            EstadoFolhas.Verificar("F2.2", ataqueFuncionou: resultado.JanelaPrincipalAbriu);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_STARTUP_HOOKS", anterior);
        }
    }
}
