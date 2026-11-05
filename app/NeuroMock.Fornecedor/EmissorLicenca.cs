using System;
using System.Security.Cryptography;
using NeuroMock.Core.Licenciamento;

namespace NeuroMock.Fornecedor;

/// <summary>Emissão de licença assinada para a máquina de um cliente (v1).</summary>
public static class EmissorLicenca
{
    public static Licenca Criar(ECDsa chavePrivada, string cliente, DateOnly validade, string maquina)
    {
        var assinatura = chavePrivada.SignData(
            FormatoLicenca.Conteudo(cliente, validade, maquina),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return new Licenca(cliente, validade, maquina, Convert.ToBase64String(assinatura));
    }

    public static string Emitir(ECDsa chavePrivada, string cliente, DateOnly validade, string maquina) =>
        ArquivoLicenca.Serializar(Criar(chavePrivada, cliente, validade, maquina));
}
