using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NeuroMock.Core.Integridade;

/// <summary>
/// Manifesto de integridade do pacote (v2): o SHA-256 de cada arquivo publicado e a assinatura do
/// fornecedor sobre essa lista. Fica na raiz do pacote, ao lado do executável, e é gerado na
/// esteira depois do publish, com a mesma chave privada que emite as licenças.
/// </summary>
public sealed record Manifesto(IReadOnlyList<EntradaManifesto> Arquivos, string Assinatura);

/// <summary>Um arquivo do pacote: caminho relativo à raiz, com '/', e o SHA-256 em hexadecimal minúsculo.</summary>
public sealed record EntradaManifesto(string Caminho, string Sha256);

// Mesmo padrão da licença: serialização gerada em tempo de compilação, campo ausente ou nulo
// vira JSON inválido.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(Manifesto))]
internal sealed partial class ContextoJsonManifesto : JsonSerializerContext
{
}
