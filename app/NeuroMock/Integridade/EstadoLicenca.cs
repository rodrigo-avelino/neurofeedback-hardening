using System;
using System.Threading;

namespace NeuroMock.Integridade;

/// <summary>
/// Estado de licença da v3 (folha F2.3): não há uma flag "licenciado" única em memória para o
/// atacante virar. A decisão fica numa forma redundante e codificada, duas palavras cujo XOR tem
/// de dar a marca; editar um único <c>bool</c> não existe como alvo. A consistência é um sinal
/// fraco de propósito: quem manda é a reverificação ao vivo da licença (<c>VerificadorLicenca</c>)
/// que as respostas atrasadas fazem no momento sorteado. Juntas, as duas tiram valor de editar na
/// memória a decisão que abriu a tela: a conta é refeita do zero e discorda.
/// </summary>
internal static class EstadoLicenca
{
    // Valor arbitrário: só serve para a redundância não aparecer como "licenciado=true" legível.
    private const long Marca = unchecked((long)0x56336C69635F6D6E);

    private static long _a;
    private static long _b;

    /// <summary>Registra a decisão de licença em forma redundante, sem um bool de vida longa.</summary>
    public static void Definir(bool licenciado)
    {
        var nonce = Random.Shared.NextInt64();
        Volatile.Write(ref _a, nonce);
        // Licenciado: as duas palavras guardam a marca no XOR. Não licenciado: XOR qualquer outro.
        Volatile.Write(ref _b, licenciado ? nonce ^ Marca : nonce);
    }

    /// <summary>Verdadeiro só se a decisão redundante continua dizendo "licenciado".</summary>
    public static bool Consistente() => (Volatile.Read(ref _a) ^ Volatile.Read(ref _b)) == Marca;
}
