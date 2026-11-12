using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Folha H2.1 (arvore 2, roubo do algoritmo): localizar o modulo do algoritmo no binario ofuscado.
/// Ataque de caixa preta: a v3 renomeia apenas os membros internos dos namespaces do algoritmo e
/// preserva a API publica (KeepPublicApi). Basta abrir o NeuroMock.Core.dll e procurar o namespace
/// do DSP pelos nomes, que continuam em claro. O ataque "funcionou" se o modulo e localizavel pela
/// API publica: o namespace Processamento traz os tipos do pipeline com nome legivel e o ponto de
/// entrada do DSP (ProcessadorSinal.Processar) esta exposto.
/// </summary>
public sealed class T_H2_1
{
    private const string NamespaceAlgoritmo = "NeuroMock.Core.Processamento";

    [Fact(DisplayName = "T-H2.1 Localizar o modulo do algoritmo no Core ofuscado")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();
        var localizado = ModuloLocalizavel(copia.Caminho("NeuroMock.Core.dll"));

        EstadoFolhas.Verificar("H2.1", ataqueFuncionou: localizado);
    }

    private static bool ModuloLocalizavel(string caminhoAssembly)
    {
        using var modulo = ModuleDefMD.Load(File.ReadAllBytes(caminhoAssembly));

        // Tipos publicos do pipeline. A renomeacao da v3 ("so membros internos" + KeepPublicApi)
        // nao toca neles, entao continuam com nome legivel no binario ofuscado.
        string[] tiposEsperados =
        {
            "ProcessadorSinal", "Fft", "FiltroBiquad",
            "ParametrosProcessamento", "ResultadoJanela", "Banda",
        };

        var tiposPublicosNoNamespace = modulo.GetTypes()
            .Where(t => t.IsPublic && t.Namespace?.String == NamespaceAlgoritmo)
            .Select(t => t.Name.String)
            .ToHashSet(StringComparer.Ordinal);

        // Modulo localizado: o namespace do algoritmo esta em claro com todos os tipos do pipeline.
        if (!tiposEsperados.All(tiposPublicosNoNamespace.Contains))
            return false;

        // E o ponto de entrada do DSP expoe o metodo publico que roda o pipeline.
        var processador = modulo.GetTypes().FirstOrDefault(
            t => t.FullName == NamespaceAlgoritmo + ".ProcessadorSinal");
        return processador is not null
            && processador.Methods.Any(m => m.IsPublic && m.Name == "Processar");
    }
}
