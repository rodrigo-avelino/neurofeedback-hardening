using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NeuroMock.Core.Processamento;

namespace NeuroMock.Core.Sessao;

/// <summary>Acumula as janelas de uma sessão de treino e grava o resumo em CSV no disco local.</summary>
public sealed class RegistroSessao
{
    private readonly List<ResultadoJanela> _janelas = new();

    public static string PastaPadrao => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeuroMock", "sessoes");

    public DateTime Inicio { get; } = DateTime.Now;

    public int Janelas => _janelas.Count;

    public int Recompensas => _janelas.Count(j => j.Recompensa);

    public void Adicionar(ResultadoJanela janela) => _janelas.Add(janela);

    /// <summary>Grava o CSV na pasta indicada e devolve o caminho do arquivo.</summary>
    public string Salvar(string pasta)
    {
        Directory.CreateDirectory(pasta);
        var caminho = Path.Combine(pasta, $"sessao-{Inicio:yyyyMMdd-HHmmss}.csv");
        var csv = new StringBuilder();
        csv.AppendLine("janela,teta,alfa,smr,beta,indice,indice_suavizado,limiar,recompensa");
        for (var i = 0; i < _janelas.Count; i++)
        {
            var j = _janelas[i];
            csv.AppendLine(string.Join(',',
                i.ToString(CultureInfo.InvariantCulture),
                Numero(j.Teta), Numero(j.Alfa), Numero(j.Smr), Numero(j.Beta),
                Numero(j.Indice), Numero(j.IndiceSuavizado), Numero(j.Limiar),
                j.Recompensa ? "1" : "0"));
        }

        File.WriteAllText(caminho, csv.ToString());
        return caminho;
    }

    private static string Numero(double valor) => valor.ToString("G6", CultureInfo.InvariantCulture);
}
