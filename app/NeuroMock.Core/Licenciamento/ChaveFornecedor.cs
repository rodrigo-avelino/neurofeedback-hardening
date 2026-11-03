using System;
using System.Security.Cryptography;

namespace NeuroMock.Core.Licenciamento;

/// <summary>
/// Chave pública do fornecedor, embutida no cliente. Ela só verifica: emitir exige a chave
/// privada, que nunca entra no repo nem no binário. Trocar esta chave pela de um atacante é
/// adulterar o binário (ramo G1), que a v1 ainda não protege.
/// </summary>
public static class ChaveFornecedor
{
    // SubjectPublicKeyInfo (DER, base64) de uma chave ECDSA P-256, gerada por
    // app/ferramentas/emitir-licenca.cs gerar-chave.
    private const string PublicaSpkiBase64 =
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEwpDBdLhwkYa8n2BF20HPCmyGMrFmYk0vmTnNrWfV/17IgsKCTTiM3fGYl41YB0hiGfwl2kaiW3Bdnma/yzRGgg==";

    public static ECDsa Carregar()
    {
        var chave = ECDsa.Create();
        chave.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicaSpkiBase64), out _);
        return chave;
    }
}
