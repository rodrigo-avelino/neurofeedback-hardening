using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using NeuroMock.Core.Licenciamento;
using NeuroMock.Fornecedor;

namespace RegressaoMate;

/// <summary>
/// O artefato atacado: o zip da release, o mesmo que o red team recebe. Cada teste trabalha numa
/// cópia própria, para adulterar binário e licença sem afetar os outros.
/// </summary>
public static class Alvo
{
    public const string Executavel = "NeuroMock.exe";
    public const string TituloJanelaPrincipal = "NeuroMock";
    public const string ArquivoLicenca = "licenca.json";

    private static readonly Lazy<string> PastaExtraida = new(Extrair);

    public static CopiaDoAlvo NovaCopia()
    {
        var destino = Path.Combine(Ambiente.PastaTrabalho, "copias", Guid.NewGuid().ToString("N")[..8]);
        CopiarPasta(PastaExtraida.Value, destino);
        return new CopiaDoAlvo(destino);
    }

    private static string Extrair()
    {
        var pasta = Path.Combine(Ambiente.PastaTrabalho, "alvo");
        if (Directory.Exists(pasta))
            Directory.Delete(pasta, recursive: true);
        ZipFile.ExtractToDirectory(Ambiente.Artefato, pasta);
        if (!File.Exists(Path.Combine(pasta, Executavel)))
            throw new InvalidOperationException($"{Executavel} não está na raiz do zip {Ambiente.Artefato}");
        return pasta;
    }

    private static void CopiarPasta(string origem, string destino)
    {
        Directory.CreateDirectory(destino);
        foreach (var pasta in Directory.GetDirectories(origem, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destino, Path.GetRelativePath(origem, pasta)));
        foreach (var arquivo in Directory.GetFiles(origem, "*", SearchOption.AllDirectories))
            File.Copy(arquivo, Path.Combine(destino, Path.GetRelativePath(origem, arquivo)));
    }
}

/// <summary>Uma cópia descartável do alvo, onde o teste faz o que o atacante faria.</summary>
public sealed class CopiaDoAlvo : IDisposable
{
    public static readonly TimeSpan LimitePadrao = TimeSpan.FromSeconds(45);

    internal CopiaDoAlvo(string pasta)
    {
        Pasta = pasta;
    }

    public string Pasta { get; }

    public string Caminho(string relativo) => Path.Combine(Pasta, relativo);

    public void InstalarLicenca(string conteudoJson) =>
        File.WriteAllText(Caminho(Alvo.ArquivoLicenca), conteudoJson);

    /// <summary>
    /// Licença legítima que o fornecedor emitiu para a "Clínica de Teste", em outra máquina. É a
    /// licença que um pirata copiaria (F3.1).
    /// </summary>
    public void InstalarLicencaDeTeste() =>
        File.Copy(Path.Combine(Ambiente.Fixtures, "licenca-valida.json"), Caminho(Alvo.ArquivoLicenca), overwrite: true);

    /// <summary>Licença emitida na hora pelo fornecedor para o código de máquina informado.</summary>
    public void InstalarLicencaParaAMaquina(string maquina, string chavePrivadaPem)
    {
        using var chave = ChavesFornecedor.CarregarPrivada(chavePrivadaPem);
        var validade = DateOnly.FromDateTime(DateTime.Today).AddDays(30);
        InstalarLicenca(EmissorLicenca.Emitir(chave, "Clínica de Teste", validade, maquina));
    }

    /// <summary>Código da máquina que o alvo gravou na solicitação de ativação, se gravou.</summary>
    public string? LerCodigoDaMaquina() =>
        ArquivoLicenca.LerSolicitacao(Caminho(ArquivoLicenca.NomeSolicitacao))?.Maquina;

    public void RemoverLicenca() => File.Delete(Caminho(Alvo.ArquivoLicenca));

    /// <summary>
    /// Roda o executável e observa de fora, como o atacante: espera a primeira janela aparecer
    /// (ou o processo terminar) e então encerra o processo.
    /// </summary>
    public ResultadoExecucao Executar(TimeSpan? limite = null)
    {
        var inicio = new ProcessStartInfo(Caminho(Alvo.Executavel)) { WorkingDirectory = Pasta, UseShellExecute = false };
        using var processo = Process.Start(inicio) ?? throw new InvalidOperationException("o processo do alvo não iniciou");

        var relogio = Stopwatch.StartNew();
        string? titulo = null;
        while (relogio.Elapsed < (limite ?? LimitePadrao) && !processo.HasExited)
        {
            processo.Refresh();
            if (processo.MainWindowTitle is { Length: > 0 } tituloAtual)
            {
                titulo = tituloAtual;
                break;
            }

            Thread.Sleep(100);
        }

        var encerrou = processo.HasExited;
        int? codigoSaida = encerrou ? processo.ExitCode : null;
        if (!encerrou)
        {
            processo.Kill(entireProcessTree: true);
            processo.WaitForExit(10_000);
        }

        return new ResultadoExecucao(titulo, encerrou, codigoSaida, relogio.Elapsed);
    }

    public void Dispose()
    {
        // Um processo recém-encerrado pode segurar arquivo por um instante; o que sobrar é
        // apagado pelo run-all.ps1 no começo da próxima execução.
        try
        {
            Directory.Delete(Pasta, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

/// <summary>O que deu para observar de fora numa execução do alvo.</summary>
public sealed record ResultadoExecucao(string? Titulo, bool Encerrou, int? CodigoSaida, TimeSpan Duracao)
{
    /// <summary>O treino abriu: a barreira de licença foi vencida ou não existia.</summary>
    public bool JanelaPrincipalAbriu => Titulo == Alvo.TituloJanelaPrincipal;

    /// <summary>Recusa observável: apareceu outra janela (a de ativação) ou o processo terminou sem abrir o treino.</summary>
    public bool Recusou => !JanelaPrincipalAbriu && (Titulo is not null || Encerrou);

    public override string ToString() =>
        $"janela: {Titulo ?? "nenhuma"}, encerrou: {(Encerrou ? $"sim, código {CodigoSaida}" : "não")}, em {Duracao.TotalSeconds:F1} s";
}
