using System.Text;

namespace NeuroMock.Core.Licenciamento;

/// <summary>
/// Chave da build 0: derivada só do nome do cliente por um hash conhecido (FNV-1a), sem segredo
/// e sem assinatura. Ingênua de propósito: quem lê este código escreve um keygen (folha F3.2), e
/// a validade fica fora da chave, editável em claro (F3.3).
/// </summary>
public static class GeradorChave
{
    public static string Gerar(string cliente)
    {
        uint hash = 2166136261;
        foreach (var b in Encoding.UTF8.GetBytes(cliente.Trim().ToUpperInvariant()))
        {
            hash ^= b;
            hash *= 16777619;
        }

        var hex = hash.ToString("X8");
        return $"NM-{hex[..4]}-{hex[4..]}";
    }
}
