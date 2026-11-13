using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Folha H1.1 (arvore 2, roubo do algoritmo): reconstruir o pipeline/algoritmo do DSP a partir do
/// binario ofuscado. Ataque de caixa preta: a v3 renomeia so membros internos e nao ofusca o fluxo
/// de controle, entao o corpo dos metodos decompila em claro. O ataque "funcionou" se, lendo o IL
/// do ProcessadorSinal (sem executar), da para recuperar os estagios do pipeline: filtragem biquad,
/// FFT, potencia por banda (pelos getters das bandas), indice ponderado (pelos getters dos pesos) e
/// a montagem do ResultadoJanela. Distinto da H2.1 (so localizar o modulo) e da H2.2 (reproduzir a
/// saida bit a bit).
/// </summary>
public sealed class T_H1_1
{
    [Fact(DisplayName = "T-H1.1 Reconstruir o pipeline do DSP a partir do Core ofuscado")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();
        if (copia.EhNativo)
        {
            // v4 Native AOT: nao ha NeuroMock.Core.dll para decompilar e ler o IL do pipeline. H1.1
            // segue ABERTA, agora so por engenharia reversa do binario nativo (medida na mao, fora
            // da esteira). O AOT encarece a reconstrucao, nao a fecha.
            Assert.Skip("v4 Native AOT: sem Core gerenciado para decompilar; H1.1 aberta, medida na mao no nativo.");
            return;
        }

        var reconstruiu = PipelineLegivel(copia.Caminho("NeuroMock.Core.dll"));

        EstadoFolhas.Verificar("H1.1", ataqueFuncionou: reconstruiu);
    }

    private static bool PipelineLegivel(string caminhoAssembly)
    {
        using var modulo = ModuleDefMD.Load(File.ReadAllBytes(caminhoAssembly));

        var processador = modulo.GetTypes().FirstOrDefault(
            t => t.FullName == "NeuroMock.Core.Processamento.ProcessadorSinal");
        if (processador is null)
            return false;

        // Nomes de tudo que o IL dos metodos do ProcessadorSinal chama ou le, sem executar nada.
        var chamadas = processador.Methods
            .Where(m => m.HasBody)
            .SelectMany(m => m.Body.Instructions)
            .Select(i => i.Operand)
            .OfType<IMethod>()
            .Select(m => m.FullName)
            .ToList();

        // Marcos do pipeline que precisam aparecer no IL para a reconstrucao valer.
        bool temFft = chamadas.Any(n => n.Contains("Fft::Transformar", StringComparison.Ordinal));
        bool temFiltro = chamadas.Any(n => n.Contains("FiltroBiquad::Processar", StringComparison.Ordinal));
        bool temBandas =
            chamadas.Any(n => n.Contains("get_BandaTeta", StringComparison.Ordinal)) &&
            chamadas.Any(n => n.Contains("get_BandaAlfa", StringComparison.Ordinal)) &&
            chamadas.Any(n => n.Contains("get_BandaSmr", StringComparison.Ordinal)) &&
            chamadas.Any(n => n.Contains("get_BandaBeta", StringComparison.Ordinal));
        bool temPesos =
            chamadas.Any(n => n.Contains("get_PesoTeta", StringComparison.Ordinal)) &&
            chamadas.Any(n => n.Contains("get_PesoAlfa", StringComparison.Ordinal)) &&
            chamadas.Any(n => n.Contains("get_PesoSmr", StringComparison.Ordinal)) &&
            chamadas.Any(n => n.Contains("get_PesoBeta", StringComparison.Ordinal));
        bool montaResultado = chamadas.Any(n => n.Contains("ResultadoJanela::.ctor", StringComparison.Ordinal));

        return temFft && temFiltro && temBandas && temPesos && montaResultado;
    }
}
