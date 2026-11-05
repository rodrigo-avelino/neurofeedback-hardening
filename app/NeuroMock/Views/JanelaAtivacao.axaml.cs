using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NeuroMock.Core.Licenciamento;

namespace NeuroMock.Views;

/// <summary>
/// Ativação: mostra o código da máquina e grava a solicitação de ativação ao lado do executável,
/// para o cliente mandar ao fornecedor e receber o licenca.json amarrado a esta máquina.
/// </summary>
public partial class JanelaAtivacao : Window
{
    // Usado pelo designer visual.
    public JanelaAtivacao()
        : this(AppContext.BaseDirectory)
    {
    }

    public JanelaAtivacao(string pastaAplicativo)
    {
        InitializeComponent();

        var maquina = IdentidadeMaquina.TentarAtual();
        if (maquina is null)
        {
            CodigoMaquina.Text = "indisponível";
            Instrucoes.Text = "Não foi possível identificar esta máquina. Fale com o fornecedor.";
            return;
        }

        CodigoMaquina.Text = maquina;
        var solicitacao = ArquivoLicenca.GravarSolicitacao(pastaAplicativo, maquina);
        Instrucoes.Text = solicitacao is null
            ? $"Quando receber o {ArquivoLicenca.NomeLicenca}, coloque-o na pasta do aplicativo e abra o programa de novo."
            : $"O código também está em {solicitacao}. Quando receber o {ArquivoLicenca.NomeLicenca}, coloque-o na mesma pasta e abra o programa de novo.";
    }

    private void Fechar(object? sender, RoutedEventArgs e) => Close();
}
