using System;
using System.IO;
using dnlib.DotNet;

namespace RegressaoMate;

/// <summary>
/// Apoio aos ataques estáticos (ramo G1): abre um assembly da cópia do alvo com dnlib, aplica a
/// alteração e grava por cima, como o atacante faria salvando o módulo no dnSpy.
/// </summary>
public static class Assembleia
{
    public static void Modificar(string caminhoAssembly, Action<ModuleDefMD> alteracao)
    {
        // Carrega dos bytes para não manter o arquivo aberto enquanto grava por cima.
        using var modulo = ModuleDefMD.Load(File.ReadAllBytes(caminhoAssembly));
        alteracao(modulo);
        modulo.Write(caminhoAssembly);
    }
}
