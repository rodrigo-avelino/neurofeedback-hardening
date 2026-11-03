using System;
using System.Text.Json.Serialization;

namespace NeuroMock.Core.Licenciamento;

/// <summary>
/// Conteúdo do arquivo de licença entregue ao cliente. A assinatura do fornecedor cobre cliente,
/// validade e máquina: mudar qualquer um deles invalida a licença.
/// </summary>
public sealed record Licenca(string Cliente, DateOnly Validade, string Maquina, string Assinatura);

/// <summary>Pedido de licença que o app grava quando não encontra uma válida.</summary>
public sealed record SolicitacaoAtivacao(string Maquina);

// Serialização gerada em tempo de compilação, sem reflexão: mantém aberto o caminho para
// ofuscação e Native AOT nas builds seguintes. Campo ausente ou nulo vira JSON inválido.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(Licenca))]
[JsonSerializable(typeof(SolicitacaoAtivacao))]
internal sealed partial class ContextoJsonLicenca : JsonSerializerContext
{
}
