using System;
using System.Security.Cryptography;

namespace NeuroMock.Fornecedor;

/// <summary>Par de chaves ECDSA P-256 do fornecedor: a privada em PEM (PKCS#8), a pública em SPKI base64.</summary>
public static class ChavesFornecedor
{
    public static ECDsa Gerar() => ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public static string PrivadaEmPem(ECDsa chave) => chave.ExportPkcs8PrivateKeyPem();

    public static string PublicaEmBase64(ECDsa chave) => Convert.ToBase64String(chave.ExportSubjectPublicKeyInfo());

    public static ECDsa CarregarPrivada(string pem)
    {
        var chave = ECDsa.Create();
        chave.ImportFromPem(pem);
        return chave;
    }
}
