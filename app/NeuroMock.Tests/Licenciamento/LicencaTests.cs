using System;
using System.IO;
using NeuroMock.Core.Licenciamento;
using Xunit;

namespace NeuroMock.Tests.Licenciamento;

public sealed class LicencaTests : IDisposable
{
    private static readonly DateOnly Hoje = new(2026, 11, 3);

    private readonly DirectoryInfo _pasta = Directory.CreateTempSubdirectory("neuromock-licenca-");

    public void Dispose() => _pasta.Delete(recursive: true);

    private void Gravar(string conteudo) =>
        File.WriteAllText(Path.Combine(_pasta.FullName, VerificadorLicenca.NomeArquivo), conteudo);

    private bool Valida() => VerificadorLicenca.LicencaValida(_pasta.FullName, Hoje);

    [Fact]
    public void LicencaEmitidaEhValida()
    {
        Gravar(EmissorLicenca.Emitir("Clínica Exemplo", Hoje.AddDays(30)));
        Assert.True(Valida());
    }

    [Fact]
    public void LicencaValeAteOUltimoDia()
    {
        Gravar(EmissorLicenca.Emitir("Clínica Exemplo", Hoje));
        Assert.True(Valida());
    }

    [Fact]
    public void LicencaVencidaEhRecusada()
    {
        Gravar(EmissorLicenca.Emitir("Clínica Exemplo", Hoje.AddDays(-1)));
        Assert.False(Valida());
    }

    [Fact]
    public void ChaveDeOutroClienteEhRecusada()
    {
        var chave = GeradorChave.Gerar("Clínica Exemplo");
        Gravar($$"""{"cliente":"Outra Clínica","validade":"2026-12-31","chave":"{{chave}}"}""");
        Assert.False(Valida());
    }

    [Fact]
    public void SemArquivoEhRecusada()
    {
        Assert.False(Valida());
        Assert.Null(VerificadorLicenca.Ler(_pasta.FullName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("não é json")]
    [InlineData("""{"cliente":"Clínica Exemplo"}""")]
    [InlineData("""{"cliente":null,"validade":"2026-12-31","chave":"NM-0000-0000"}""")]
    [InlineData("""{"cliente":"Clínica Exemplo","validade":"31/12/2026","chave":"NM-0000-0000"}""")]
    public void ArquivoMalformadoEhRecusado(string conteudo)
    {
        Gravar(conteudo);
        Assert.False(Valida());
    }

    [Fact]
    public void ChaveIgnoraCaixaEEspacosDoCliente()
    {
        Assert.Equal(GeradorChave.Gerar("CLÍNICA EXEMPLO"), GeradorChave.Gerar("  clínica exemplo "));
    }

    [Fact]
    public void ChaveTemFormatoDoProduto()
    {
        Assert.Matches("^NM-[0-9A-F]{4}-[0-9A-F]{4}$", GeradorChave.Gerar("Clínica Exemplo"));
    }
}
