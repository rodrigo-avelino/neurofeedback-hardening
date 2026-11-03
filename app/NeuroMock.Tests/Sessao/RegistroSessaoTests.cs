using System.IO;
using NeuroMock.Core.Processamento;
using NeuroMock.Core.Sessao;
using Xunit;

namespace NeuroMock.Tests.Sessao;

public sealed class RegistroSessaoTests
{
    [Fact]
    public void SalvarGravaCabecalhoEUmaLinhaPorJanela()
    {
        var pasta = Directory.CreateTempSubdirectory("neuromock-sessao-");
        try
        {
            var registro = new RegistroSessao();
            registro.Adicionar(new ResultadoJanela(1, 2, 3, 4, 0.5, 0.25, 0.75, false));
            registro.Adicionar(new ResultadoJanela(1, 2, 3, 4, 1.5, 1.25, 0.75, true));

            var linhas = File.ReadAllLines(registro.Salvar(pasta.FullName));

            Assert.Equal(3, linhas.Length);
            Assert.Equal("janela,teta,alfa,smr,beta,indice,indice_suavizado,limiar,recompensa", linhas[0]);
            Assert.Equal("1,1,2,3,4,1.5,1.25,0.75,1", linhas[2]);
            Assert.Equal(1, registro.Recompensas);
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }
}
