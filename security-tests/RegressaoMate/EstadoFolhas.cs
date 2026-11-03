using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;

namespace RegressaoMate;

/// <summary>
/// Estado declarado de cada folha em security-tests/estado-folhas.json e a regra que compara esse
/// estado com o resultado do ataque. A esteira fica verde com folha aberta documentada e só fica
/// vermelha em regressão ou em estado desatualizado.
/// </summary>
public static class EstadoFolhas
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Estados = new(Carregar);

    public static string Declarado(string folha) =>
        Estados.Value.TryGetValue(folha, out var estado)
            ? estado
            : throw new InvalidOperationException($"a folha {folha} não está em estado-folhas.json");

    /// <summary>Compara o resultado do ataque com o estado declarado da folha e registra no resumo.</summary>
    public static void Verificar(string folha, bool ataqueFuncionou)
    {
        var declarado = Declarado(folha);
        var (ok, mensagem) = (declarado, ataqueFuncionou) switch
        {
            ("aberta", true) => (true, "buraco documentado"),
            ("aberta", false) => (false,
                $"{folha} está declarada aberta, mas o ataque falhou: se a folha fechou, mude o estado em estado-folhas.json e na matriz"),
            ("fechada", false) => (true, "o controle resiste"),
            ("fechada", true) => (false, $"REGRESSÃO: {folha} está declarada fechada, mas o ataque funcionou"),
            _ => (false, $"{folha} está declarada '{declarado}': folha inativa não tem teste rodando"),
        };

        Resultados.RegistrarFolha(folha, declarado, ataqueFuncionou, ok);
        if (!ok)
            Assert.Fail(mensagem);
    }

    private static IReadOnlyDictionary<string, string> Carregar()
    {
        using var documento = JsonDocument.Parse(File.ReadAllText(Ambiente.ArquivoEstado));
        var estados = new Dictionary<string, string>();
        foreach (var folha in documento.RootElement.GetProperty("folhas").EnumerateObject())
        {
            var estado = folha.Value.GetString();
            if (estado is not ("aberta" or "fechada" or "inativa"))
                throw new InvalidOperationException($"estado inválido para {folha.Name}: '{estado}'");
            estados[folha.Name] = estado;
        }

        return estados;
    }
}

/// <summary>Linhas JSON que o run-all.ps1 junta no resumo por folha.</summary>
internal static class Resultados
{
    private static readonly object Trava = new();

    public static void RegistrarFolha(string folha, string declarado, bool ataqueFuncionou, bool ok) =>
        Registrar(new { folha, declarado, ataque = ataqueFuncionou ? "funcionou" : "falhou", ok });

    public static void RegistrarControle(string? motivoDaFalha) =>
        Registrar(new { controle = "positivo", ok = motivoDaFalha is null, motivo = motivoDaFalha });

    private static void Registrar(object linha)
    {
        if (Ambiente.ArquivoResultados is not { } arquivo)
            return;
        lock (Trava)
            File.AppendAllText(arquivo, JsonSerializer.Serialize(linha) + Environment.NewLine);
    }
}
