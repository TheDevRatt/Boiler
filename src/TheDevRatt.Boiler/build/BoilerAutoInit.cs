// Injected into the consuming assembly by the Boiler NuGet package (see
// TheDevRatt.Boiler.props). The module initializer runs once, at assembly load
// — before any game code — and installs Steam's native library resolver, so
// consumers get cross-platform Steam with zero setup code.

namespace TheDevRatt.Boiler.Generated
{
    internal static class BoilerAutoInit
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Initialize() => TheDevRatt.Boiler.SteamNative.Register();
    }
}
