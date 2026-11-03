using System;
using System.Text.Json;

namespace NeuroMock.Core.Licenciamento;

/// <summary>
/// Emissão de licença, o lado do fornecedor. Na build 0 ela mora no mesmo assembly que o
/// cliente recebe, porque a chave não depende de segredo nenhum: emitir e validar são a mesma
/// conta. A partir da licença assinada (v1), emitir passa a exigir a chave privada, que fica
/// fora do cliente.
/// </summary>
public static class EmissorLicenca
{
    public static Licenca Criar(string cliente, DateOnly validade) =>
        new(cliente, validade, GeradorChave.Gerar(cliente));

    public static string Emitir(string cliente, DateOnly validade) =>
        JsonSerializer.Serialize(Criar(cliente, validade), ContextoJsonLicenca.Default.Licenca);
}
