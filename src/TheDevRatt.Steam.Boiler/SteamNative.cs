using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TheDevRatt.Steam.Boiler;

/// <summary>
/// Makes Facepunch.Steamworks' native library load correctly under Godot on
/// Windows, macOS (x64 + arm64), and Linux.
///
/// Godot's .NET host does not honour NuGet's <c>runtimes/&lt;rid&gt;/native</c>
/// convention the way a normal app does, so the native <c>steam_api</c> library
/// is not found by the default P/Invoke probe. This installs a
/// <see cref="NativeLibrary.SetDllImportResolver"/> that loads the correct
/// binary (placed next to the assemblies by this package's MSBuild targets).
///
/// <see cref="Register"/> is invoked automatically at startup via a module
/// initializer the package injects into the consuming assembly, so there is
/// nothing to call by hand. It remains public for manual/early invocation.
/// </summary>
public static class SteamNative
{
    private static bool _registered;

    /// <summary>Installs the native resolver for Facepunch's platform assembly.
    /// Called automatically at startup; idempotent and safe to call again.</summary>
    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;

        // Facepunch ships a distinct managed assembly per platform (Win64 vs
        // Posix); the resolver must be attached to whichever one is loaded.
        string assemblyName = OperatingSystem.IsWindows()
            ? "Facepunch.Steamworks.Win64"
            : "Facepunch.Steamworks.Posix";

        Assembly facepunch;
        try
        {
            facepunch = Assembly.Load(assemblyName);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[SteamNative] Could not load {assemblyName}: {e.Message}");
            return;
        }

        NativeLibrary.SetDllImportResolver(facepunch, Resolve);
        Console.WriteLine($"[SteamNative] Native resolver registered for {assemblyName}.");
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName.IndexOf("steam_api", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return IntPtr.Zero;
        }

        foreach (string path in Candidates())
        {
            if (File.Exists(path) && NativeLibrary.TryLoad(path, out IntPtr handle))
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    private static string NativeFileName()
    {
        if (OperatingSystem.IsWindows())
        {
            return "steam_api64.dll";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "libsteam_api.dylib";
        }

        return "libsteam_api.so";
    }

    private static IEnumerable<string> Candidates()
    {
        string file = NativeFileName();

        // Placed here by this package's MSBuild targets, in editor and exports.
        yield return Path.Combine(AppContext.BaseDirectory, file);

        // Fallback: beside the running executable (packaged/.app layouts).
        string? exeDir = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.IsNullOrEmpty(exeDir))
        {
            yield return Path.Combine(exeDir, file);
        }
    }
}
