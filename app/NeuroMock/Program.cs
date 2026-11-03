using System;
using Avalonia;

namespace NeuroMock;

internal sealed class Program
{
    // Nada de Avalonia nem de código que dependa de SynchronizationContext antes do AppMain.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Também usado pelo designer visual.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
