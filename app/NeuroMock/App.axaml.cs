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
            // Barreira de licença da build 0: um único if no cliente decide se o treino abre.
            // É o alvo natural dos ramos G1 e G2 da árvore 1, e é assim de propósito.
            if (VerificadorLicenca.LicencaValida(AppContext.BaseDirectory))
                desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
            else
                desktop.MainWindow = new JanelaAtivacao();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
