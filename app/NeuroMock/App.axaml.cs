using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NeuroMock.Core.Licenciamento;
using NeuroMock.Integridade;
using NeuroMock.ViewModels;
using NeuroMock.Views;

namespace NeuroMock;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Integridade (v2): a primeira guarda começa cedo, em segundo plano, e só deixa o selo.
        Guardas.VarrerPacote(AppContext.BaseDirectory);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Barreira de licença: o if continua aqui, igual ao da v1. A v2 não o esconde: protege
            // o binário que o contém, com o manifesto assinado conferido por guardas e respostas
            // atrasadas espalhadas pelo app. Patchear este if (ramo G1) abre o treino, que fecha
            // sozinho alguns segundos depois.
            if (VerificadorLicenca.LicencaValida(AppContext.BaseDirectory))
                desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
            else
                desktop.MainWindow = new JanelaAtivacao(AppContext.BaseDirectory);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
