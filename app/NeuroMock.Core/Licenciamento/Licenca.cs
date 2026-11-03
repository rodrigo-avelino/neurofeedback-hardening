using System;
using System.Text.Json.Serialization;

namespace NeuroMock.Core.Licenciamento;

/// <summary>Conteúdo do arquivo de licença entregue ao cliente.</summary>
public sealed record Licenca(string Cliente, DateOnly Validade, string Chave);

// Serialização gerada em tempo de compilação, sem reflexão: mantém aberto o caminho para
// ofuscação e Native AOT nas builds seguintes. Campo ausente ou nulo vira JSON inválido.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(Licenca))]
internal sealed partial class ContextoJsonLicenca : JsonSerializerContext
{
}
