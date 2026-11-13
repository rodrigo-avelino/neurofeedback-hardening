using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using dnlib.DotNet;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Folha H1.2 (arvore 2, roubo do algoritmo): extrair os parametros/pesos do DSP.
/// Ataque de caixa preta: a config vive como recurso embutido no NeuroMock.Core.dll. Basta abrir
/// o assembly e ler o recurso; nenhum segredo precisa ser derivado, os pesos e as bandas estao
/// em JSON claro. O ataque "funcionou" se o recurso existe e os pesos/bandas sao legiveis.
/// </summary>
public sealed class T_H1_2
{
    [Fact(DisplayName = "T-H1.2 Extrair parametros/pesos do DSP da config embutida")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();
        if (copia.EhNativo)
        {
            // v4 Native AOT: nao ha NeuroMock.Core.dll para abrir e ler o recurso embutido. A folha
            // ja estava fechada na v3 (parametros fora de arquivo em claro); o AOT reforca.
            EstadoFolhas.Verificar("H1.2", ataqueFuncionou: false);
            return;
        }

        var vazou = ParametrosVazaram(copia.Caminho("NeuroMock.Core.dll"));

        EstadoFolhas.Verificar("H1.2", ataqueFuncionou: vazou);
    }

    private static bool ParametrosVazaram(string caminhoAssembly)
    {
        using var modulo = ModuleDefMD.Load(File.ReadAllBytes(caminhoAssembly));

        var recurso = modulo.Resources
            .OfType<EmbeddedResource>()
            .FirstOrDefault(r => r.Name.String.EndsWith("parametros.json", System.StringComparison.Ordinal));
        if (recurso is null)
            return false;

        var json = Encoding.UTF8.GetString(recurso.CreateReader().ToArray());
        using var doc = JsonDocument.Parse(json);
        var raiz = doc.RootElement;

        // o segredo vazou se os pesos do indice e as faixas das bandas estao em claro
        string[] chavesEsperadas =
        {
            "pesoTeta", "pesoAlfa", "pesoSmr", "pesoBeta",
            "bandaTeta", "bandaAlfa", "bandaSmr", "bandaBeta",
        };
        return chavesEsperadas.All(chave => raiz.TryGetProperty(chave, out _));
    }
}
