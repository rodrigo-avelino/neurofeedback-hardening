using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NeuroMock.Core.Integridade;
using NeuroMock.Core.Licenciamento;

namespace NeuroMock.Integridade;

/// <summary>
/// As duas guardas de integridade da v2 (desenho em docs/06-plano-iteracoes.md). Cada uma roda em
/// segundo plano, num ponto diferente do app, e só deixa o resultado (o selo): nenhuma decide
/// nada onde roda. Quem age são as respostas atrasadas, em outros pontos (MainWindow e
/// MainWindowViewModel), e elas falham fechado: selo ausente conta como adulteração, então tirar
/// uma guarda não libera o app.
/// </summary>
/// <remarks>
/// As duas guardas são diferentes de propósito, para um mesmo patch não derrubar as duas: a
/// primeira varre o pacote inteiro com o verificador e a chave do Core; a segunda confere os
/// arquivos de onde o código em execução foi carregado, com implementação e chave próprias do App.
/// </remarks>
internal static class Guardas
{
    private static Task<bool>? _pacote;
    private static Task<bool>? _carregados;

    /// <summary>Selo da guarda do pacote inteiro; <c>null</c> se ela não foi iniciada.</summary>
    public static Task<bool>? Pacote => Volatile.Read(ref _pacote);

    /// <summary>Selo da guarda dos arquivos carregados; <c>null</c> se ela não foi iniciada.</summary>
    public static Task<bool>? Carregados => Volatile.Read(ref _carregados);

    /// <summary>Guarda 1: todos os arquivos do pacote contra o manifesto, com a chave pública do Core.</summary>
    public static void VarrerPacote(string pastaPacote) =>
        Volatile.Write(ref _pacote, Task.Run(() =>
        {
            using var chave = ChaveFornecedor.Carregar();
            return VerificadorIntegridade.PacoteIntegro(pastaPacote, chave);
        }));

    /// <summary>Guarda 2: os arquivos que estão de fato em execução, com a cópia da chave que mora no App.</summary>
    public static void ConferirCarregados(string pastaPacote) =>
        Volatile.Write(ref _carregados, Task.Run(() => CarregadosConferem(pastaPacote)));

    private static bool CarregadosConferem(string pastaPacote)
    {
        try
        {
            if (ArquivoManifesto.Ler(pastaPacote) is not { } manifesto)
                return false;

            // Aqui a assinatura confere sobre os hashes declarados; depois, cada arquivo em
            // execução tem de bater com o seu.
            using var chave = ChaveEmbutida.Carregar();
            if (!chave.VerifyData(
                    FormatoManifesto.Conteudo(manifesto.Arquivos),
                    Convert.FromBase64String(manifesto.Assinatura),
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                return false;

            var comparacao = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var declarados = manifesto.Arquivos.ToDictionary(entrada => entrada.Caminho, entrada => entrada.Sha256, comparacao);

            // Em Native AOT (v4) todo o código gerenciado em execução (App, Core, criptografia,
            // runtime) está dentro do único executável nativo: não há assemblies em disco nem
            // deps.json/runtimeconfig.json (Assembly.Location devolveria vazio, IL3000). "De onde o
            // código foi carregado" colapsa para a imagem nativa, e conferir o hash dela cobre de
            // uma vez o que antes eram vários arquivos. Falha fechada: sem o caminho da imagem,
            // conta como adulteração.
            if (Environment.ProcessPath is not { Length: > 0 } imagem)
                return false;
            var relativo = Path.GetRelativePath(pastaPacote, imagem).Replace(Path.DirectorySeparatorChar, '/');
            return declarados.TryGetValue(relativo, out var esperado) && ArquivoManifesto.Sha256(imagem) == esperado;
        }
        catch (Exception e) when (e is ArgumentException or FormatException or IOException
                                      or UnauthorizedAccessException or CryptographicException)
        {
            return false;
        }
    }

}
