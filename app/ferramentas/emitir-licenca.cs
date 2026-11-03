#:project ../NeuroMock.Fornecedor/NeuroMock.Fornecedor.csproj

// Ferramenta do fornecedor (a partir da v1).
//
// Gerar o par de chaves, uma vez. A privada fica fora de qualquer repositório git:
//   dotnet run app/ferramentas/emitir-licenca.cs -- gerar-chave <arquivo.pem fora do repo>
//
// Emitir uma licença para a máquina do cliente (o código, ou o solicitacao-ativacao.json dele):
//   dotnet run app/ferramentas/emitir-licenca.cs -- emitir <chave.pem> "Nome do cliente" AAAA-MM-DD <código ou arquivo>

using System;
using System.Globalization;
using System.IO;
using NeuroMock.Core.Licenciamento;
using NeuroMock.Fornecedor;

return args switch
{
    ["gerar-chave", var destino] => GerarChave(destino),
    ["emitir", var arquivoChave, var cliente, var validade, var maquina] => Emitir(arquivoChave, cliente, validade, maquina),
    _ => Uso(),
};

static int GerarChave(string destino)
{
    var caminho = Path.GetFullPath(destino);
    if (File.Exists(caminho))
        return Erro($"{caminho} já existe; chave privada não é sobrescrita");
    if (DentroDeRepositorioGit(caminho))
        return Erro($"{caminho} está dentro de um repositório git; a chave privada não pode morar em repo");

    Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
    using var chave = ChavesFornecedor.Gerar();
    File.WriteAllText(caminho, ChavesFornecedor.PrivadaEmPem(chave));
    Console.WriteLine($"Chave privada gravada em {caminho}. Guarde com backup: sem ela, nenhuma licença nova sai.");
    Console.WriteLine("Chave pública (para ChaveFornecedor.cs):");
    Console.WriteLine(ChavesFornecedor.PublicaEmBase64(chave));
    return 0;
}

static int Emitir(string arquivoChave, string cliente, string textoValidade, string maquinaOuArquivo)
{
    if (!DateOnly.TryParseExact(textoValidade, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var validade))
        return Erro("a validade vai no formato AAAA-MM-DD");

    var maquina = File.Exists(maquinaOuArquivo) ? ArquivoLicenca.LerSolicitacao(maquinaOuArquivo)?.Maquina : maquinaOuArquivo;
    if (string.IsNullOrWhiteSpace(maquina))
        return Erro("a solicitação de ativação não tem código de máquina");

    using var chave = ChavesFornecedor.CarregarPrivada(File.ReadAllText(arquivoChave));
    Console.WriteLine(EmissorLicenca.Emitir(chave, cliente, validade, maquina));
    return 0;
}

static bool DentroDeRepositorioGit(string caminho)
{
    for (var pasta = Path.GetDirectoryName(caminho); pasta is not null; pasta = Path.GetDirectoryName(pasta))
    {
        if (Directory.Exists(Path.Combine(pasta, ".git")) || File.Exists(Path.Combine(pasta, ".git")))
            return true;
    }

    return false;
}

static int Uso()
{
    Console.Error.WriteLine("uso: emitir-licenca.cs gerar-chave <arquivo.pem fora do repo>");
    Console.Error.WriteLine("     emitir-licenca.cs emitir <chave.pem> \"Nome do cliente\" AAAA-MM-DD <código ou solicitacao-ativacao.json>");
    return 1;
}

static int Erro(string mensagem)
{
    Console.Error.WriteLine($"erro: {mensagem}");
    return 1;
}
