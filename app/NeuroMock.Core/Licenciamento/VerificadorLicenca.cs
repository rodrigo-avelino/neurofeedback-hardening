using System;
using System.Security.Cryptography;

namespace NeuroMock.Core.Licenciamento;

/// <summary>
/// Verificação de licença da v1: a assinatura do fornecedor (ECDSA P-256) tem de conferir com a
/// chave pública embutida, e a licença tem de ser desta máquina e estar no prazo. Sem segredo no
/// cliente não há keygen (F3.2); campo editado quebra a assinatura (F3.3); licença de outra
/// máquina é recusada (F3.1). A decisão ainda é um único ponto no cliente, sem integridade do
/// binário: o ramo G1 continua aberto de propósito, é o alvo da v2.
/// </summary>
public static class VerificadorLicenca
{
    public static bool LicencaValida(string pastaAplicativo)
    {
        if (IdentidadeMaquina.TentarAtual() is not { } maquina)
            return false;

        using var chavePublica = ChaveFornecedor.Carregar();
        return LicencaValida(pastaAplicativo, DateOnly.FromDateTime(DateTime.Today), maquina, chavePublica);
    }

    public static bool LicencaValida(string pastaAplicativo, DateOnly hoje, string maquina, ECDsa chavePublica)
    {
        var licenca = ArquivoLicenca.Ler(pastaAplicativo);
        return licenca is not null
            && licenca.Validade >= hoje
            && licenca.Maquina == maquina
            && AssinaturaConfere(licenca, chavePublica);
    }

    private static bool AssinaturaConfere(Licenca licenca, ECDsa chavePublica)
    {
        if (!FormatoLicenca.CamposValidos(licenca.Cliente, licenca.Maquina))
            return false;

        byte[] assinatura;
        try
        {
            assinatura = Convert.FromBase64String(licenca.Assinatura);
        }
        catch (FormatException)
        {
            return false;
        }

        return chavePublica.VerifyData(
            FormatoLicenca.Conteudo(licenca.Cliente, licenca.Validade, licenca.Maquina),
            assinatura,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }
}
