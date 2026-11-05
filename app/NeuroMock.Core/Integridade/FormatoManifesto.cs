using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroMock.Core.Integridade;

/// <summary>
/// Os bytes que a assinatura do manifesto cobre. Emissor e verificadores montam exatamente o mesmo
/// conteúdo: um cabeçalho próprio (assinatura de licença não serve de assinatura de manifesto, nem
/// o contrário, mesmo com a mesma chave) e uma linha por arquivo, em ordem ordinal de caminho.
/// Caminho e hash são validados para nenhum manifesto ter duas leituras.
/// </summary>
public static class FormatoManifesto
{
    private const string Cabecalho = "NeuroMock/manifesto/v1";

    public static byte[] Conteudo(IEnumerable<EntradaManifesto> arquivos)
    {
        var entradas = arquivos.ToList();
        foreach (var entrada in entradas)
        {
            if (entrada is null || !CaminhoValido(entrada.Caminho) || !HashValido(entrada.Sha256))
                throw new ArgumentException("entrada do manifesto com caminho ou hash inválido");
        }

        entradas.Sort((a, b) => string.CompareOrdinal(a.Caminho, b.Caminho));
        var texto = new StringBuilder(Cabecalho);
        for (var i = 0; i < entradas.Count; i++)
        {
            if (i > 0 && entradas[i].Caminho == entradas[i - 1].Caminho)
                throw new ArgumentException($"arquivo repetido no manifesto: {entradas[i].Caminho}");
            texto.Append('\n').Append(entradas[i].Sha256).Append(' ').Append(entradas[i].Caminho);
        }

        return Encoding.UTF8.GetBytes(texto.ToString());
    }

    /// <summary>Caminho relativo à raiz do pacote, separado por '/', que não sai da pasta.</summary>
    public static bool CaminhoValido(string? caminho) =>
        !string.IsNullOrEmpty(caminho)
        && !caminho.Any(char.IsControl)
        && !caminho.Contains('\\')
        && !caminho.Contains(':') // letra de unidade ou fluxo alternativo do NTFS
        && caminho.Split('/').All(parte => parte.Length > 0 && parte != "." && parte != "..");

    /// <summary>SHA-256 em hexadecimal minúsculo, como sai de <see cref="ArquivoManifesto.Sha256"/>.</summary>
    public static bool HashValido(string? sha256) =>
        sha256 is { Length: 64 } && sha256.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
