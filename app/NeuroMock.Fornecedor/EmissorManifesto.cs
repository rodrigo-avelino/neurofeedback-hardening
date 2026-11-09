using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NeuroMock.Core.Integridade;
using NeuroMock.Core.Licenciamento;

namespace NeuroMock.Fornecedor;

/// <summary>
/// Assinatura do pacote publicado (v2): o manifesto com o SHA-256 de cada arquivo, assinado com a
/// chave privada do fornecedor. Roda na esteira, sobre a pasta do publish, antes de empacotar.
/// </summary>
public static class EmissorManifesto
{
    // Arquivos da raiz que mudam na máquina do cliente (ou que são o próprio manifesto) e por isso
    // ficam fora dele.
    private static readonly string[] ForaDoManifesto =
        [ArquivoManifesto.Nome, ArquivoLicenca.NomeLicenca, ArquivoLicenca.NomeSolicitacao];

    public static Manifesto Criar(string pastaPacote, ECDsa chavePrivada)
    {
        var raiz = Path.GetFullPath(pastaPacote);
        var entradas = Directory.EnumerateFiles(raiz, "*", SearchOption.AllDirectories)
            .Select(arquivo => Path.GetRelativePath(raiz, arquivo).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(relativo => !ForaDoManifesto.Contains(relativo, StringComparer.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .Select(relativo => new EntradaManifesto(relativo, ArquivoManifesto.Sha256(Path.Combine(raiz, relativo))))
            .ToList();
        if (entradas.Count == 0)
            throw new ArgumentException($"{raiz} não tem arquivos para assinar");

        var assinatura = chavePrivada.SignData(
            FormatoManifesto.Conteudo(entradas),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return new Manifesto(entradas, Convert.ToBase64String(assinatura));
    }

    /// <summary>Grava o manifesto assinado na raiz do pacote e devolve o manifesto gravado.</summary>
    public static Manifesto Gravar(string pastaPacote, ECDsa chavePrivada)
    {
        var manifesto = Criar(pastaPacote, chavePrivada);
        File.WriteAllText(Path.Combine(pastaPacote, ArquivoManifesto.Nome), ArquivoManifesto.Serializar(manifesto));
        return manifesto;
    }
}
