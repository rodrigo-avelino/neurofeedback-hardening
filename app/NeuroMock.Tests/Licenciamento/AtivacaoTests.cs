using System.IO;
using NeuroMock.Core.Licenciamento;
using Xunit;

namespace NeuroMock.Tests.Licenciamento;

public sealed class AtivacaoTests
{
    [Fact]
    public void CodigoDaMaquinaEhEstavelEIgnoraCaixaEEspacos()
    {
        Assert.Equal(IdentidadeMaquina.Codigo("ABC-123"), IdentidadeMaquina.Codigo("  abc-123 "));
    }

    [Fact]
    public void CodigoDaMaquinaTemCincoGruposHexadecimais()
    {
        Assert.Matches("^[0-9A-F]{4}(-[0-9A-F]{4}){4}$", IdentidadeMaquina.Codigo("qualquer identificador"));
    }

    [Fact]
    public void EstaMaquinaTemCodigo()
    {
        Assert.NotNull(IdentidadeMaquina.TentarAtual());
    }

    [Fact]
    public void SolicitacaoGravadaVoltaComOMesmoCodigo()
    {
        var pasta = Directory.CreateTempSubdirectory("neuromock-ativacao-");
        try
        {
            var caminho = ArquivoLicenca.GravarSolicitacao(pasta.FullName, "AAAA-BBBB-CCCC-DDDD-EEEE");
            Assert.NotNull(caminho);
            Assert.Equal("AAAA-BBBB-CCCC-DDDD-EEEE", ArquivoLicenca.LerSolicitacao(caminho!)?.Maquina);
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }
}
