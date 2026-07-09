// Injected into the consuming assembly by the Boiler NuGet package (see
// TheDevRatt.Steam.Boiler.props). The module initializer runs once, at assembly
// load, before any game code, and installs Steam's native library resolver, so
// consumers get cross-platform Steam with zero setup code.

// CA2255: using [ModuleInitializer] from a library is exactly the point here,
// it is how the package installs its resolver without any consumer code.
#pragma warning disable CA2255

namespace TheDevRatt.Steam.Boiler.Generated
{
    internal static class BoilerAutoInit
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Initialize() => TheDevRatt.Steam.Boiler.SteamNative.Register();
    }
}

#pragma warning restore CA2255
