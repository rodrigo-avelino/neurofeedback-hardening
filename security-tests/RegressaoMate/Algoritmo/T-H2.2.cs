using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;
using Reimpl = RegressaoMate.ReimplH22;

namespace RegressaoMate;

/// <summary>
/// Folha H2.2 (arvore 2): compreender a logica a ponto de reimplementar o algoritmo.
/// Ataque de caixa preta: carrega o ProcessadorSinal ORIGINAL do binario do alvo, roda ele e a
/// reimplementacao do red team no mesmo sinal e compara janela a janela. O ataque "funcionou" se
/// a reimplementacao reproduz a saida do original bit a bit (o criterio do oraculo, automatizado).
/// </summary>
public sealed class T_H2_2
{
    [Fact(DisplayName = "T-H2.2 Reimplementar o algoritmo e reproduzir a saida do binario")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();
        if (copia.EhNativo)
        {
            // v4 Native AOT: nao da para Assembly.Load do NeuroMock.Core.dll (nao existe) para rodar
            // o ProcessadorSinal original por reflexao. H2.2 segue ABERTA, agora so por engenharia
            // reversa do binario nativo ou dirigindo o exe como caixa preta: medida na mao, fora da
            // esteira. So o servidor fecha de verdade.
            Assert.Skip("v4 Native AOT: sem Core gerenciado para o oraculo; H2.2 aberta, medida na mao no nativo.");
            return;
        }

        var reproduz = ReproduzOAlgoritmo(copia.Caminho("NeuroMock.Core.dll"));

        EstadoFolhas.Verificar("H2.2", ataqueFuncionou: reproduz);
    }

    private static bool ReproduzOAlgoritmo(string caminhoCore)
    {
        var amostras = SinalSintetico(5000);

        // 1) original, carregado do binario do alvo (sem referencia em tempo de compilacao)
        var asm = Assembly.Load(System.IO.File.ReadAllBytes(caminhoCore));
        var tipoProc = asm.GetType("NeuroMock.Core.Processamento.ProcessadorSinal")
            ?? throw new InvalidOperationException("ProcessadorSinal nao encontrado no alvo");
        var comPadrao = tipoProc.GetMethod("ComParametrosPadrao", BindingFlags.Public | BindingFlags.Static)!;
        var processar = tipoProc.GetMethod("Processar")!;
        var proc = comPadrao.Invoke(null, null)!;

        var chamarProcessar = CriarTrampolim(tipoProc, processar);
        var origLista = ((IEnumerable)chamarProcessar(proc, amostras)).Cast<object>().ToList();

        // 2) reimplementacao do red team
        var nossa = Reimpl.ProcessadorSinal.ComParametrosPadrao().Processar(amostras);

        if (origLista.Count == 0 || origLista.Count != nossa.Count)
            return false;

        for (int i = 0; i < origLista.Count; i++)
        {
            double[] o = NumerosDouble(origLista[i]);
            bool oRecompensa = ValorBool(origLista[i]);
            double[] n =
            {
                nossa[i].PotenciaTeta, nossa[i].PotenciaAlfa, nossa[i].PotenciaSmr, nossa[i].PotenciaBeta,
                nossa[i].Indice, nossa[i].IndiceSuavizado, nossa[i].Limiar,
            };

            if (o.Length != n.Length || oRecompensa != nossa[i].Recompensa)
                return false;
            for (int k = 0; k < n.Length; k++)
                if (o[k] != n[k])        // bit a bit: mesma matematica, mesma ordem de operacoes
                    return false;
        }

        return true;
    }

    // Trampolim em IL: reflexao nao passa ReadOnlySpan<double> (ref struct). Emite
    //   object Chamar(object p, double[] a) => ((ProcessadorSinal)p).Processar(new ReadOnlySpan<double>(a));
    private static Func<object, double[], object> CriarTrampolim(Type tipoProc, MethodInfo processar)
    {
        var dm = new DynamicMethod("ChamarProcessar", typeof(object),
            new[] { typeof(object), typeof(double[]) }, typeof(T_H2_2).Module, skipVisibility: true);
        var il = dm.GetILGenerator();
        var ctorSpan = typeof(ReadOnlySpan<double>).GetConstructor(new[] { typeof(double[]) })!;
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, tipoProc);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Newobj, ctorSpan);
        il.Emit(OpCodes.Callvirt, processar);
        il.Emit(OpCodes.Ret);
        return (Func<object, double[], object>)dm.CreateDelegate(typeof(Func<object, double[], object>));
    }

    private static double[] NumerosDouble(object resultado) =>
        resultado.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(double))
            .Select(p => (double)p.GetValue(resultado)!)
            .ToArray();

    private static bool ValorBool(object resultado) =>
        (bool)resultado.GetType().GetProperties()
            .First(p => p.PropertyType == typeof(bool)).GetValue(resultado)!;

    private static double[] SinalSintetico(int n)
    {
        const double fs = 250.0;
        var s = new double[n];
        for (int i = 0; i < n; i++)
        {
            double t = i / fs;
            s[i] = 1.0 * Math.Sin(2 * Math.PI * 6 * t)
                + 0.8 * Math.Sin(2 * Math.PI * 10 * t)
                + 0.5 * Math.Sin(2 * Math.PI * 13 * t)
                + 0.9 * Math.Sin(2 * Math.PI * 20 * t)
                + 0.3 * Math.Sin(2 * Math.PI * 60 * t);
        }
        return s;
    }
}
