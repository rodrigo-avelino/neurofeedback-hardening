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

        // Execução (v3): vigia o depurador desde o começo (F2.1). Como as guardas da v2, só fixa o
        // selo; quem age são as respostas atrasadas.
        GuardasRuntime.IniciarVigilanciaDepurador();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Barreira de licença: a decisão continua direto no if, e o salto condicional logo
            // depois da chamada (o que o ramo G1 ataca na F1.2) fica igual ao da v1/v2. A v2 protege
            // o binário que o contém (manifesto assinado, guardas e respostas atrasadas). A v3 marca
            // o estado de forma redundante nos dois ramos (F2.3), sem uma flag "licenciado" única de
            // vida longa; a palavra final é a reverificação atrasada da licença, do zero. Patchear
            // este if (G1) ou abrir o treino por hook/debugger (G2) abre a janela, que fecha sozinha
            // alguns segundos depois.
            if (VerificadorLicenca.LicencaValida(AppContext.BaseDirectory))
            {
                EstadoLicenca.Definir(true);
                desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
            }
            else
            {
                EstadoLicenca.Definir(false);
                desktop.MainWindow = new JanelaAtivacao(AppContext.BaseDirectory);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
