using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroMock.Core.Processamento;
using NeuroMock.Core.Sensor;
using NeuroMock.Core.Sessao;

namespace NeuroMock.ViewModels;

/// <summary>
/// Tela de treino: a cada tique lê do sensor simulado o que chegou desde o anterior, passa pelo
/// processador e mostra o índice, o limiar e a recompensa da última janela.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    public const double EscalaIndice = 1.2;

    private static readonly TimeSpan Intervalo = TimeSpan.FromMilliseconds(250);

    private readonly DispatcherTimer _relogio;
    private IFonteSinal? _sensor;
    private ProcessadorSinal? _processador;
    private RegistroSessao? _registro;

    public MainWindowViewModel()
    {
        Situacao = "Pronto. Inicie uma sessão para começar o treino.";
        TextoIndice = "Índice: -   Limiar: -";
        ResumoBandas = "";
        _relogio = new DispatcherTimer { Interval = Intervalo };
        _relogio.Tick += (_, _) => Atualizar();
    }

    [ObservableProperty]
    public partial string Situacao { get; set; }

    [ObservableProperty]
    public partial double IndiceNaEscala { get; set; }

    [ObservableProperty]
    public partial string TextoIndice { get; set; }

    [ObservableProperty]
    public partial string ResumoBandas { get; set; }

    [ObservableProperty]
    public partial bool Recompensa { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(IniciarCommand), nameof(EncerrarCommand))]
    public partial bool EmSessao { get; set; }

    [RelayCommand(CanExecute = nameof(PodeIniciar))]
    private void Iniciar()
    {
        _sensor = new SimuladorSensor(semente: Environment.TickCount);
        _processador = ProcessadorSinal.ComParametrosPadrao();
        _registro = new RegistroSessao();
        EmSessao = true;
        Situacao = "Sessão em andamento.";
        _relogio.Start();
    }

    private bool PodeIniciar() => !EmSessao;

    [RelayCommand(CanExecute = nameof(PodeEncerrar))]
    private void Encerrar()
    {
        _relogio.Stop();
        EmSessao = false;
        Recompensa = false;
        if (_registro is { Janelas: > 0 } registro)
        {
            var arquivo = registro.Salvar(RegistroSessao.PastaPadrao);
            Situacao = $"Sessão encerrada: {registro.Recompensas} recompensas em {registro.Janelas} janelas. Gravada em {arquivo}";
        }
        else
        {
            Situacao = "Sessão encerrada sem dados.";
        }
    }

    private bool PodeEncerrar() => EmSessao;

    private void Atualizar()
    {
        if (_sensor is null || _processador is null || _registro is null)
            return;

        var quantidade = (int)Math.Round(_sensor.TaxaAmostragem * Intervalo.TotalSeconds);
        foreach (var janela in _processador.Processar(_sensor.Ler(quantidade)))
        {
            _registro.Adicionar(janela);
            IndiceNaEscala = Math.Min(janela.IndiceSuavizado, EscalaIndice);
            Recompensa = janela.Recompensa;
            TextoIndice = $"Índice: {janela.IndiceSuavizado:F2}   Limiar: {janela.Limiar:F2}";
            var total = janela.Teta + janela.Alfa + janela.Smr + janela.Beta;
            ResumoBandas = total > 0
                ? $"θ {janela.Teta / total:P0}   α {janela.Alfa / total:P0}   SMR {janela.Smr / total:P0}   β {janela.Beta / total:P0}"
                : "";
        }
    }
}
