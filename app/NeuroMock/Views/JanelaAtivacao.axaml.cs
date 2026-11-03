using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NeuroMock.Views;

public partial class JanelaAtivacao : Window
{
    public JanelaAtivacao()
    {
        InitializeComponent();
    }

    private void Fechar(object? sender, RoutedEventArgs e) => Close();
}
