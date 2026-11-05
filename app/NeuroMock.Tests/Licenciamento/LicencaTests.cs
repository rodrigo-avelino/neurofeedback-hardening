using System;
using System.IO;
using System.Security.Cryptography;
using NeuroMock.Core.Licenciamento;
using NeuroMock.Fornecedor;
using Xunit;

namespace NeuroMock.Tests.Licenciamento;

public sealed class LicencaTests : IDisposable
{
    private const string EstaMaquina = "AAAA-BBBB-CCCC-DDDD-EEEE";
    private static readonly DateOnly Hoje = new(2026, 11, 3);

    // Chave efêmera do "fornecedor" de cada teste: a real fica fora do repo.
    private readonly ECDsa _fornecedor = ChavesFornecedor.Gerar();
    private readonly DirectoryInfo _pasta = Directory.CreateTempSubdirectory("neuromock-licenca-");

    public void Dispose()
    {
        _fornecedor.Dispose();
        _pasta.Delete(recursive: true);
    }

    private Licenca Emitir(DateOnly validade, string maquina = EstaMaquina, ECDsa? chave = null) =>
        EmissorLicenca.Criar(chave ?? _fornecedor, "Clínica Exemplo", validade, maquina);

    private void Gravar(Licenca licenca) => Gravar(ArquivoLicenca.Serializar(licenca));

    private void Gravar(string conteudo) =>
        File.WriteAllText(Path.Combine(_pasta.FullName, ArquivoLicenca.NomeLicenca), conteudo);

    private bool Valida() => VerificadorLicenca.LicencaValida(_pasta.FullName, Hoje, EstaMaquina, _fornecedor);

    [Fact]
    public void LicencaEmitidaParaEstaMaquinaEhValida()
    {
        Gravar(Emitir(Hoje.AddDays(30)));
        Assert.True(Valida());
    }

    [Fact]
    public void LicencaValeAteOUltimoDia()
    {
        Gravar(Emitir(Hoje));
        Assert.True(Valida());
    }

    [Fact]
    public void LicencaVencidaEhRecusada()
    {
        Gravar(Emitir(Hoje.AddDays(-1)));
        Assert.False(Valida());
    }

    [Fact] // F3.1
    public void LicencaDeOutraMaquinaEhRecusada()
    {
        Gravar(Emitir(Hoje.AddDays(30), maquina: "0000-1111-2222-3333-4444"));
        Assert.False(Valida());
    }

    [Fact] // F3.1 + F3.3: copiar a licença de outra máquina e trocar o código por este
    public void MaquinaEditadaEhRecusada()
    {
        Gravar(Emitir(Hoje.AddDays(30), maquina: "0000-1111-2222-3333-4444") with { Maquina = EstaMaquina });
        Assert.False(Valida());
    }

    [Fact] // F3.3
    public void ValidadeEditadaEhRecusada()
    {
        Gravar(Emitir(Hoje.AddDays(-1)) with { Validade = Hoje.AddYears(5) });
        Assert.False(Valida());
    }

    [Fact] // F3.3
    public void ClienteEditadoEhRecusado()
    {
        Gravar(Emitir(Hoje.AddDays(30)) with { Cliente = "Outra Clínica" });
        Assert.False(Valida());
    }

    [Fact] // F3.2: o keygen do atacante assina com a própria chave
    public void LicencaAssinadaPorOutraChaveEhRecusada()
    {
        using var atacante = ChavesFornecedor.Gerar();
        Gravar(Emitir(Hoje.AddDays(30), chave: atacante));
        Assert.False(Valida());
    }

    [Fact]
    public void SemArquivoEhRecusada()
    {
        Assert.False(Valida());
        Assert.Null(ArquivoLicenca.Ler(_pasta.FullName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("não é json")]
    [InlineData("""{"cliente":"Clínica Exemplo","validade":"2026-12-31"}""")]
    [InlineData("""{"cliente":null,"validade":"2026-12-31","maquina":"AAAA-BBBB-CCCC-DDDD-EEEE","assinatura":"AAAA"}""")]
    [InlineData("""{"cliente":"Clínica Exemplo","validade":"31/12/2026","maquina":"AAAA-BBBB-CCCC-DDDD-EEEE","assinatura":"AAAA"}""")]
    [InlineData("""{"cliente":"Clínica Exemplo","validade":"2026-12-31","maquina":"AAAA-BBBB-CCCC-DDDD-EEEE","assinatura":"%%%"}""")]
    [InlineData("""{"cliente":"Clínica Exemplo","validade":"2026-12-31","maquina":"AAAA-BBBB-CCCC-DDDD-EEEE","assinatura":""}""")]
    public void ArquivoMalformadoEhRecusado(string conteudo)
    {
        Gravar(conteudo);
        Assert.False(Valida());
    }

    [Fact]
    public void CampoComCaractereDeControleNaoEhEmitido()
    {
        Assert.Throws<ArgumentException>(() => EmissorLicenca.Criar(_fornecedor, "Clínica\nFalsa", Hoje, EstaMaquina));
    }

    [Fact]
    public void ChavePublicaEmbutidaEhP256()
    {
        using var chave = ChaveFornecedor.Carregar();
        Assert.Equal(256, chave.KeySize);
    }
}
