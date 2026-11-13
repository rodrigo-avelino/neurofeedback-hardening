using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NeuroMock.Core.Licenciamento;

namespace NeuroMock.Integridade;

/// <summary>
/// Guardas de execução da v3 (ramo G2 da árvore 1): anti-debug (F2.1) e detecção de hook por
/// integridade em runtime (F2.2). Como as guardas de pacote da v2, elas só detectam e deixam o
/// resultado disponível; quem age são as duas respostas atrasadas da v2 (MainWindow e
/// MainWindowViewModel), que agregam estes sinais mais a reverificação da licença de forma
/// independente uma da outra. As primitivas são compartilhadas (como <c>Guardas</c>), a decisão
/// não: um patch numa primitiva a desliga nas duas respostas, mas derrubar todas exige achar
/// várias, no mesmo custo da v2.
/// </summary>
/// <remarks>
/// Os sinais são conservadores: na dúvida não acusam, porque num app clínico um falso positivo não
/// pode fechar o treino de um paciente legítimo. O teto é honesto: um depurador de kernel ou um
/// detour sob medida com a ferramenta renomeada ainda passam; o ganho é o custo de achar e manter.
/// </remarks>
internal static class GuardasRuntime
{
    // ---------- F2.1: anti-debug ----------
    private static int _viuDepurador;

    /// <summary>Selo pegajoso: um depurador foi visto na janela inicial, mesmo que tenha saído depois.</summary>
    public static bool DepuradorVisto => Volatile.Read(ref _viuDepurador) != 0;

    /// <summary>Amostra o depurador por alguns segundos no começo e fixa o selo se o vir.</summary>
    public static void IniciarVigilanciaDepurador() => _ = Task.Run(async () =>
    {
        for (var i = 0; i < 20; i++)
        {
            if (DepuradorPresente())
            {
                Volatile.Write(ref _viuDepurador, 1);
                return;
            }

            await Task.Delay(200).ConfigureAwait(false);
        }
    });

    /// <summary>Depurador anexado agora, gerenciado (.NET) ou nativo (o x64dbg/WinDbg da F2.1).</summary>
    public static bool DepuradorPresente()
    {
        try
        {
            if (Debugger.IsAttached || Debugger.IsLogging())
                return true;
            if (OperatingSystem.IsWindows())
            {
                if (IsDebuggerPresent())
                    return true;
                if (CheckRemoteDebuggerPresent(GetCurrentProcess(), out var remoto) && remoto)
                    return true;
            }
        }
        catch
        {
            // Na dúvida não acusa.
        }

        return false;
    }

    // ---------- F2.2: anti-hook por integridade em runtime ----------

    /// <summary>
    /// Hook se um framework de hook está carregado ou se o método de decisão da licença começa com
    /// um salto incondicional (o detour que Harmony/MonoMod escrevem no início do método).
    /// </summary>
    public static bool HookDetectado()
    {
        if (FrameworkDeHookCarregado())
            return true;

        // Em Native AOT (v4) não há JIT nem precode: Harmony e MonoMod reescrevem IL/JIT e não se
        // aplicam, e DOTNET_STARTUP_HOOKS não é honrado pelo executável nativo. O detour em IL que
        // a F2.2 pegava deixa de ter alvo (neutralizado por construção). O passeio pelo prólogo só
        // faz sentido com código dinâmico (JIT), então fora dele não acusa.
        if (!RuntimeFeature.IsDynamicCodeSupported)
            return false;

        try
        {
            return ResolverPrologo() is { } prologo && ComecaComSalto(prologo);
        }
        catch
        {
            // Não conseguiu ler o código: conservador, não acusa.
            return false;
        }
    }

    private static bool FrameworkDeHookCarregado()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var nome = assembly.GetName().Name ?? "";
            if (nome.Contains("Harmony", StringComparison.OrdinalIgnoreCase)
                || nome.StartsWith("MonoMod", StringComparison.OrdinalIgnoreCase)
                || nome.Contains("RuntimeDetour", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    // O código legítimo começa com o prólogo da função (montagem da pilha), nunca com um salto
    // incondicional: E9 (jmp rel32) ou FF 25 (jmp [rip]). Um detour reescreve o início do método
    // com esse salto. Comparar com um baseline do endereço não serve, porque o runtime recompila o
    // método (tier-0 -> tier-1) sozinho na janela da resposta: o endereço e os bytes mudam sem hook
    // nenhum, o que dava falso positivo. O padrão do salto vale nos dois níveis do JIT.
    private static bool ComecaComSalto(byte[] prologo) =>
        prologo[0] == 0xE9 || (prologo[0] == 0xFF && prologo[1] == 0x25);

    // Segue a indireção FF 25 (jmp [rip+disp]) do precode até o código real e devolve os primeiros
    // bytes de onde o método de fato começa.
    private static byte[]? ResolverPrologo()
    {
        var metodo = typeof(VerificadorLicenca).GetMethod(nameof(VerificadorLicenca.LicencaValida), [typeof(string)]);
        if (metodo is null)
            return null;

        RuntimeHelpers.PrepareMethod(metodo.MethodHandle);
        var entrada = metodo.MethodHandle.GetFunctionPointer();
        var codigo = entrada;
        if (Marshal.ReadByte(entrada, 0) == 0xFF && Marshal.ReadByte(entrada, 1) == 0x25)
        {
            var deslocamento = Marshal.ReadInt32(entrada, 2);
            codigo = Marshal.ReadIntPtr(entrada + 6 + deslocamento);
        }

        var prologo = new byte[16];
        Marshal.Copy(codigo, prologo, 0, prologo.Length);
        return prologo;
    }

    [DllImport("kernel32.dll")]
    private static extern bool IsDebuggerPresent();

    [DllImport("kernel32.dll")]
    private static extern bool CheckRemoteDebuggerPresent(nint processo, out bool presente);

    [DllImport("kernel32.dll")]
    private static extern nint GetCurrentProcess();
}
