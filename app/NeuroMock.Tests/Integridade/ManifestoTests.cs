using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NeuroMock.Core.Integridade;
using NeuroMock.Core.Licenciamento;
using NeuroMock.Fornecedor;
using Xunit;

namespace NeuroMock.Tests.Integridade;

public sealed class ManifestoTests : IDisposable
{
    // Chave efêmera do "fornecedor" de cada teste e um pacote de mentira, com subpasta.
    private readonly ECDsa _fornecedor = ChavesFornecedor.Gerar();
    private readonly DirectoryInfo _pacote = Directory.CreateTempSubdirectory("neuromock-pacote-");

    public ManifestoTests()
    {
        Escrever("NeuroMock.exe", "apphost");
        Escrever("NeuroMock.dll", "app");
        Escrever("NeuroMock.Core.dll", "core");
        Escrever("runtimes/win-x64/native/libexemplo.dll", "nativo");
        EmissorManifesto.Gravar(_pacote.FullName, _fornecedor);
    }

    public void Dispose()
    {
        _fornecedor.Dispose();
        _pacote.Delete(recursive: true);
    }

    private string Caminho(string relativo) => Path.Combine(_pacote.FullName, relativo);

    private void Escrever(string relativo, string conteudo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Caminho(relativo))!);
        File.WriteAllText(Caminho(relativo), conteudo);
    }

    private void TrocarUmByte(string relativo)
    {
        var bytes = File.ReadAllBytes(Caminho(relativo));
        bytes[0] ^= 0x01;
        File.WriteAllBytes(Caminho(relativo), bytes);
    }

    private Manifesto LerManifesto() => ArquivoManifesto.Ler(_pacote.FullName)!;

    private void GravarManifesto(Manifesto manifesto) =>
        File.WriteAllText(Caminho(ArquivoManifesto.Nome), ArquivoManifesto.Serializar(manifesto));

    private bool Integro(ECDsa? chave = null) => VerificadorIntegridade.PacoteIntegro(_pacote.FullName, chave ?? _fornecedor);

    [Fact]
    public void PacoteAssinadoEhIntegro()
    {
        Assert.True(Integro());
    }

    [Fact]
    public void ManifestoListaCadaArquivoPeloCaminhoRelativoComBarra()
    {
        Assert.Equal(
            ["NeuroMock.Core.dll", "NeuroMock.dll", "NeuroMock.exe", "runtimes/win-x64/native/libexemplo.dll"],
            LerManifesto().Arquivos.Select(entrada => entrada.Caminho));
    }

    [Fact]
    public void HashDoArquivoEhOSha256EmHexadecimalMinusculo()
    {
        Escrever("abc.txt", "abc");
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", ArquivoManifesto.Sha256(Caminho("abc.txt")));
    }

    [Theory] // F1.1 a F1.3: qualquer patch muda os bytes de um arquivo do pacote
    [InlineData("NeuroMock.dll")]
    [InlineData("NeuroMock.Core.dll")]
    [InlineData("NeuroMock.exe")]
    [InlineData("runtimes/win-x64/native/libexemplo.dll")]
    public void UmByteAlteradoEmQualquerArquivoQuebraAIntegridade(string arquivo)
    {
        TrocarUmByte(arquivo);
        Assert.False(Integro());
    }

    [Fact]
    public void ArquivoRemovidoQuebraAIntegridade()
    {
        File.Delete(Caminho("NeuroMock.Core.dll"));
        Assert.False(Integro());
    }

    [Fact] // o atacante adultera o Core e corrige o hash dele no manifesto
    public void HashCorrigidoNoManifestoNaoAdianta()
    {
        TrocarUmByte("NeuroMock.Core.dll");
        var manifesto = LerManifesto();
        GravarManifesto(manifesto with
        {
            Arquivos = manifesto.Arquivos
                .Select(entrada => entrada.Caminho == "NeuroMock.Core.dll"
                    ? entrada with { Sha256 = ArquivoManifesto.Sha256(Caminho(entrada.Caminho)) }
                    : entrada)
                .ToList(),
        });

        Assert.False(Integro());
    }

    [Fact] // o atacante adultera o Core e o tira do manifesto
    public void EntradaRemovidaDoManifestoQuebraAIntegridade()
    {
        TrocarUmByte("NeuroMock.Core.dll");
        var manifesto = LerManifesto();
        GravarManifesto(manifesto with { Arquivos = manifesto.Arquivos.Where(entrada => entrada.Caminho != "NeuroMock.Core.dll").ToList() });
        Assert.False(Integro());
    }

    [Fact] // troca de chave (G1): o atacante assina de novo o pacote adulterado com a chave dele
    public void ManifestoAssinadoPorOutraChaveEhRecusado()
    {
        using var atacante = ChavesFornecedor.Gerar();
        TrocarUmByte("NeuroMock.Core.dll");
        EmissorManifesto.Gravar(_pacote.FullName, atacante);

        Assert.False(Integro());

        // Com a chave do atacante no lugar da do fornecedor, o mesmo pacote passaria: por isso o
        // app guarda duas cópias da chave, uma em cada assembly, e cada guarda usa uma.
        Assert.True(Integro(atacante));
    }

    [Fact]
    public void SemManifestoNaoHaIntegridade()
    {
        File.Delete(Caminho(ArquivoManifesto.Nome));
        Assert.Null(ArquivoManifesto.Ler(_pacote.FullName));
        Assert.False(Integro());
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("não é json")]
    [InlineData("""{"arquivos":[]}""")]
    [InlineData("""{"arquivos":null,"assinatura":"AAAA"}""")]
    [InlineData("""{"arquivos":[null],"assinatura":"AAAA"}""")]
    [InlineData("""{"arquivos":[{"caminho":"NeuroMock.dll"}],"assinatura":"AAAA"}""")]
    [InlineData("""{"arquivos":[],"assinatura":"%%%"}""")]
    [InlineData("""{"arquivos":[{"caminho":"../fora.dll","sha256":"0000000000000000000000000000000000000000000000000000000000000000"}],"assinatura":"AAAA"}""")]
    public void ManifestoMalformadoEhRecusado(string conteudo)
    {
        File.WriteAllText(Caminho(ArquivoManifesto.Nome), conteudo);
        Assert.False(Integro());
    }

    [Theory]
    [InlineData("")]
    [InlineData("../fora.dll")]
    [InlineData("pasta/../../fora.dll")]
    [InlineData("./NeuroMock.dll")]
    [InlineData("/absoluto.dll")]
    [InlineData("C:/absoluto.dll")]
    [InlineData("NeuroMock.dll:fluxo")]
    [InlineData("pasta\\arquivo.dll")]
    [InlineData("pasta//arquivo.dll")]
    [InlineData("quebra\nde-linha.dll")]
    public void CaminhoQuePodeSairDoPacoteNaoEntraNoFormato(string caminho)
    {
        Assert.False(FormatoManifesto.CaminhoValido(caminho));
        Assert.Null(ArquivoManifesto.CaminhoNoPacote(_pacote.FullName, caminho));
        Assert.Throws<ArgumentException>(() => FormatoManifesto.Conteudo([new EntradaManifesto(caminho, new string('a', 64))]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD")]
    [InlineData("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad\n")]
    public void HashForaDoFormatoNaoEntraNoConteudo(string sha256)
    {
        Assert.Throws<ArgumentException>(() => FormatoManifesto.Conteudo([new EntradaManifesto("NeuroMock.dll", sha256)]));
    }

    [Fact]
    public void ConteudoAssinadoNaoDependeDaOrdemDasEntradas()
    {
        var entradas = LerManifesto().Arquivos;
        Assert.Equal(FormatoManifesto.Conteudo(entradas), FormatoManifesto.Conteudo(entradas.Reverse()));
    }

    [Fact]
    public void ArquivoRepetidoNaoEntraNoConteudo()
    {
        var entrada = LerManifesto().Arquivos[0];
        Assert.Throws<ArgumentException>(() => FormatoManifesto.Conteudo([entrada, entrada]));
    }

    [Fact] // licença e solicitação mudam na máquina do cliente; o próprio manifesto não se lista
    public void LicencaSolicitacaoEManifestoFicamDeFora()
    {
        Escrever(ArquivoLicenca.NomeLicenca, "{}");
        Escrever(ArquivoLicenca.NomeSolicitacao, "{}");
        var caminhos = EmissorManifesto.Gravar(_pacote.FullName, _fornecedor).Arquivos.Select(entrada => entrada.Caminho).ToList();

        Assert.DoesNotContain(ArquivoLicenca.NomeLicenca, caminhos);
        Assert.DoesNotContain(ArquivoLicenca.NomeSolicitacao, caminhos);
        Assert.DoesNotContain(ArquivoManifesto.Nome, caminhos);

        Escrever(ArquivoLicenca.NomeLicenca, """{"outra":"licença"}""");
        Assert.True(Integro());
    }

    [Fact] // o runtime só carrega o que o deps.json manda, e o deps.json está no manifesto
    public void ArquivoNovoQueOManifestoNaoListaNaoMudaOResultado()
    {
        Escrever("extra.dll", "nao listado");
        Assert.True(Integro());
    }

    [Fact]
    public void PastaVaziaNaoEhAssinada()
    {
        var vazia = Directory.CreateTempSubdirectory("neuromock-vazio-");
        try
        {
            Assert.Throws<ArgumentException>(() => EmissorManifesto.Criar(vazia.FullName, _fornecedor));
        }
        finally
        {
            vazia.Delete(recursive: true);
        }
    }
}
