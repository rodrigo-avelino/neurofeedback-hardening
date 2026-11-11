using System;
using System.IO;
using System.Reflection;
using HarmonyLib;

// O runtime do .NET procura um tipo chamado StartupHook (sem namespace) com um metodo estatico
// Initialize() sem parametros. Ele roda ANTES do Main do app alvo.
// Ataque F2.2: grampear (hook) VerificadorLicenca.LicencaValida para sempre liberar, sem tocar
// em nenhum arquivo do pacote (a integridade da v2 nao detecta alteracao em memoria).
internal sealed class StartupHook
{
    private static string _dir;

    public static void Initialize()
    {
        _dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        // o hook e o 0Harmony.dll vivem na mesma pasta; resolve as dependencias de la
        AppDomain.CurrentDomain.AssemblyResolve += Resolver;
        Patch();
    }

    private static Assembly Resolver(object sender, ResolveEventArgs args)
    {
        string nome = new AssemblyName(args.Name).Name;
        string candidato = Path.Combine(_dir, nome + ".dll");
        return File.Exists(candidato) ? Assembly.LoadFrom(candidato) : null;
    }

    private static void Patch()
    {
        // carrega o Core do alvo (mesma pasta do NeuroMock.exe) para achar o metodo
        string core = Path.Combine(AppContext.BaseDirectory, "NeuroMock.Core.dll");
        Assembly asm = Assembly.LoadFrom(core);
        Type tipo = asm.GetType("NeuroMock.Core.Licenciamento.VerificadorLicenca");
        MethodInfo alvo = tipo.GetMethod("LicencaValida", new[] { typeof(string) });

        var harmony = new Harmony("red.f2.2");
        MethodInfo prefixo = typeof(StartupHook).GetMethod(
            nameof(SemprePassa), BindingFlags.Static | BindingFlags.NonPublic);
        harmony.Patch(alvo, prefix: new HarmonyMethod(prefixo));
    }

    // prefixo Harmony: define o retorno como true e pula o metodo original
    private static bool SemprePassa(ref bool __result)
    {
        __result = true;
        return false;
    }
}
