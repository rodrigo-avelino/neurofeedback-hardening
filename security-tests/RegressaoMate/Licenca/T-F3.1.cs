using Xunit;

namespace RegressaoMate;

/// <summary>
/// Folha F3.1 - Reusar uma licença válida de outra máquina.
/// A v0 não tem binding de hardware: a licença legítima da "Clínica de Teste"
/// (emitida para outra máquina) abre o treino aqui, verbatim, sem alteração nenhuma.
/// </summary>
public sealed class T_F3_1
{
    [Fact(DisplayName = "T-F3.1 Reusar licença válida de outra máquina")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();
        copia.RemoverLicenca();           // o pirata não tem licença própria
        copia.InstalarLicencaDeTeste();   // reusa a licença de "outra clínica", verbatim

        var resultado = copia.Executar();

        // folha declarada "aberta": ataque funcionar = teste passa (buraco documentado)
        EstadoFolhas.Verificar("F3.1", ataqueFuncionou: resultado.JanelaPrincipalAbriu);
    }
}
