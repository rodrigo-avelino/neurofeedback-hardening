using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace NeuroMock.Core.Licenciamento;

/// <summary>
/// Código da máquina que a licença amarra (v1). Vem de um identificador do sistema passado por
/// hash, o que esconde o valor bruto e o torna próprio deste produto. Quem administra a máquina
/// consegue trocar esse identificador: o binding encarece copiar a licença (F3.1), não impede.
/// </summary>
public static class IdentidadeMaquina
{
    private const string Prefixo = "NeuroMock/maquina/v1|";

    /// <summary>Código desta máquina, ou <c>null</c> se o sistema não oferece um identificador.</summary>
    public static string? TentarAtual()
    {
        try
        {
            return IdentificadorDoSistema() is { Length: > 0 } bruto ? Codigo(bruto) : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return null;
        }
    }

    /// <summary>Converte um identificador bruto no código de cinco grupos que vai na licença.</summary>
    public static string Codigo(string identificadorBruto)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(Prefixo + identificadorBruto.Trim().ToLowerInvariant()));
        var hex = Convert.ToHexString(hash, 0, 10);
        return string.Join('-', Enumerable.Range(0, 5).Select(i => hex.Substring(i * 4, 4)));
    }

    private static string? IdentificadorDoSistema()
    {
        if (OperatingSystem.IsWindows())
            return MachineGuid();

        // Fora do Windows (testes e devcontainer), o identificador do systemd.
        foreach (var caminho in new[] { "/etc/machine-id", "/var/lib/dbus/machine-id" })
        {
            if (File.Exists(caminho))
                return File.ReadAllText(caminho);
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private static string? MachineGuid()
    {
        using var raiz = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var chave = raiz.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        return chave?.GetValue("MachineGuid") as string;
    }
}
