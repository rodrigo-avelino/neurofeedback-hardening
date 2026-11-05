using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NeuroMock.Core.Licenciamento;

/// <summary>
/// Os bytes que a assinatura cobre. Emissor e verificador montam exatamente o mesmo conteúdo, e
/// campo com caractere de controle é recusado para nenhuma licença ter duas leituras.
/// </summary>
public static class FormatoLicenca
{
    private const string Cabecalho = "NeuroMock/licenca/v1";

    public static byte[] Conteudo(string cliente, DateOnly validade, string maquina)
    {
        if (!CamposValidos(cliente, maquina))
            throw new ArgumentException("cliente e máquina não podem ser vazios nem ter caracteres de controle");

        var data = validade.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return Encoding.UTF8.GetBytes($"{Cabecalho}\n{cliente}\n{data}\n{maquina}");
    }

    public static bool CamposValidos(string cliente, string maquina) =>
        !string.IsNullOrWhiteSpace(cliente)
        && !string.IsNullOrWhiteSpace(maquina)
        && !cliente.Any(char.IsControl)
        && !maquina.Any(char.IsControl);
}
