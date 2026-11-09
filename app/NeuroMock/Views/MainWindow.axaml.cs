using System;
using Avalonia.Controls;
using NeuroMock.Integridade;
using NeuroMock.ViewModels;
#if !DEBUG
using Avalonia.Threading;
#endif

namespace NeuroMock.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Integridade (v2): a segunda guarda nasce com a janela do treino.
        Guardas.ConferirCarregados(AppContext.BaseDirectory);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
#if !DEBUG
        ArmarResposta();
#endif
    }

    // Fechar a janela no meio do treino grava o que já foi coletado.
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel { EmSessao: true } viewModel)
            viewModel.EncerrarCommand.Execute(null);

        base.OnClosing(e);
    }

#if !DEBUG
    // Resposta atrasada 1 (v2). Alguns segundos depois de o treino abrir, num momento sorteado,
    // confere os selos das duas guardas; se faltar um ou algum acusar adulteração, fecha a janela
    // por um caminho comum, longe do ponto da checagem. A conferência é repetida de propósito na
    // resposta 2 (MainWindowViewModel): uma função compartilhada seria um ponto único para
    // neutralizar. Só existe em Release: o app de Debug não tem manifesto assinado.
    private void ArmarResposta()
    {
        var relogio = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Random.Shared.Next(4_000, 10_000)) };
        relogio.Tick += (_, _) =>
        {
            var pacote = Guardas.Pacote;
            var carregados = Guardas.Carregados;
            if (pacote is { IsCompleted: false } || carregados is { IsCompleted: false })
            {
                relogio.Interval = TimeSpan.FromSeconds(1); // guarda ainda trabalhando: volta daqui a pouco
                return;
            }

            relogio.Stop();
            if (pacote is not { IsCompletedSuccessfully: true, Result: true }
                || carregados is not { IsCompletedSuccessfully: true, Result: true })
                Close();
        };
        relogio.Start();
    }
#endif
}
