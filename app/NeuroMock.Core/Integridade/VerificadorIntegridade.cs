using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace NeuroMock.Core.Integridade;

/// <summary>
/// Verificação do pacote inteiro contra o manifesto (v2). Em vez de comparar hash com hash, ela
/// remonta o conteúdo do manifesto com os hashes reais dos arquivos listados e confere a
/// assinatura do fornecedor sobre ele. Um byte alterado em qualquer arquivo listado, um arquivo
/// faltando ou uma entrada tirada da lista invalidam a assinatura; corrigir os hashes no
/// manifesto não adianta, porque eles nem são lidos aqui. Assinar de novo exige a chave privada,
/// que não está no cliente.
/// </summary>
/// <remarks>
/// O resultado não decide nada no ponto em que é calculado: quem age são as respostas atrasadas
/// do app. Arquivo novo que o manifesto não lista não entra na conta: o runtime só carrega o que
/// o NeuroMock.deps.json manda, e ele está no manifesto.
/// </remarks>
public static class VerificadorIntegridade
{
    // Hash de arquivo ausente: formato válido, mas nunca bate com o que o fornecedor assinou.
    private static readonly string HashDeAusente = new('0', 64);

    public static bool PacoteIntegro(string pastaPacote, ECDsa chavePublica)
    {
        try
        {
            if (ArquivoManifesto.Ler(pastaPacote) is not { } manifesto)
                return false;

            var reais = manifesto.Arquivos.Select(entrada =>
                entrada is null ? throw new ArgumentException("entrada nula no manifesto")
                    : entrada with { Sha256 = HashReal(pastaPacote, entrada.Caminho) });

            return chavePublica.VerifyData(
                FormatoManifesto.Conteudo(reais),
                Convert.FromBase64String(manifesto.Assinatura),
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (Exception e) when (e is ArgumentException or FormatException or IOException
                                      or UnauthorizedAccessException or CryptographicException)
        {
            return false;
        }
    }

    private static string HashReal(string pastaPacote, string caminhoRelativo) =>
        ArquivoManifesto.CaminhoNoPacote(pastaPacote, caminhoRelativo) is { } completo && File.Exists(completo)
            ? ArquivoManifesto.Sha256(completo)
            : HashDeAusente;
}
