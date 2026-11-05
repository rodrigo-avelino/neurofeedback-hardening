#:project ../NeuroMock.Fornecedor/NeuroMock.Fornecedor.csproj
// App de arquivo único nasce com PublishAot ligado, o que puxa o pacote do compilador AOT para o
// restore (e para a auditoria de vulnerabilidades). A ferramenta não precisa de AOT.
#:property PublishAot=false

// Ferramenta do fornecedor (a partir da v2): assina o pacote publicado, gravando na raiz dele o
// manifesto.json com o SHA-256 de cada arquivo e a assinatura ECDSA P-256 (a mesma chave das
// licenças). A esteira roda no job build-release, depois do publish e antes de empacotar:
//   dotnet run app/ferramentas/assinar-pacote.cs -- <chave.pem> <pasta publicada>
//
// Sem o manifesto assinado, o app de Release fecha sozinho alguns segundos depois de abrir o
// treino: para testar um publish local, assine a pasta com a chave do fornecedor.

using System;
using System.IO;
using NeuroMock.Core.Integridade;
using NeuroMock.Fornecedor;

return args switch
{
    [var arquivoChave, var pasta] => Assinar(arquivoChave, pasta),
    _ => Uso(),
};

static int Assinar(string arquivoChave, string pasta)
{
    if (!File.Exists(arquivoChave))
        return Erro($"chave privada não encontrada: {arquivoChave}");
    if (!Directory.Exists(pasta))
        return Erro($"pasta publicada não encontrada: {pasta}");

    using var chave = ChavesFornecedor.CarregarPrivada(File.ReadAllText(arquivoChave));
    var manifesto = EmissorManifesto.Gravar(pasta, chave);
    Console.WriteLine($"{Path.Combine(Path.GetFullPath(pasta), ArquivoManifesto.Nome)}: {manifesto.Arquivos.Count} arquivos assinados.");
    return 0;
}

static int Uso()
{
    Console.Error.WriteLine("uso: assinar-pacote.cs <chave.pem> <pasta publicada>");
    return 1;
}

static int Erro(string mensagem)
{
    Console.Error.WriteLine($"erro: {mensagem}");
    return 1;
}
