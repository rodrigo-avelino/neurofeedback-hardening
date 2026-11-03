using Avalonia.Controls;
using NeuroMock.ViewModels;

namespace NeuroMock.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Fechar a janela no meio do treino grava o que já foi coletado.
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel { EmSessao: true } viewModel)
            viewModel.EncerrarCommand.Execute(null);

        base.OnClosing(e);
    }
}
