using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NeuroMock.Core.Licenciamento;
using NeuroMock.ViewModels;
using NeuroMock.Views;

namespace NeuroMock;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Barreira de licença: um único if no cliente decide se o treino abre. A v1 deixou a
            // licença forte (assinada e presa à máquina), mas esse if continua sem proteção: é o
            // alvo dos ramos G1 e G2 da árvore 1, que a v2 ataca.
            if (VerificadorLicenca.LicencaValida(AppContext.BaseDirectory))
                desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
            else
                desktop.MainWindow = new JanelaAtivacao(AppContext.BaseDirectory);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
