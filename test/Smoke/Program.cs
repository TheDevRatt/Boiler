// CI smoke test: prove the bundled Steam native loads on this OS.
//
// A CI runner has no Steam client, so SteamClient.Init cannot fully succeed —
// and that's fine. What we assert is that loading the native does NOT fail with
// "library not found" or "entry point not found" (which would mean the wrong
// architecture, a missing binary, or a managed/native version mismatch). Steam
// simply not being logged in is a PASS: the native loaded.

using System;
using Steamworks;
using TheDevRatt.Steam.Boiler;

// Auto-registration also runs via the injected module initializer; calling it
// explicitly keeps the test deterministic regardless of initializer timing.
SteamNative.Register();

try
{
    // Init returns void and throws on failure.
    SteamClient.Init(480U);
    Console.WriteLine($"NATIVE-OK: Steam initialized ({SteamClient.Name}).");
    SteamClient.Shutdown();
    return 0;
}
catch (Exception e)
{
    string detail = e.ToString();
    bool nativeProblem =
        detail.Contains("entry point", StringComparison.OrdinalIgnoreCase) ||
        detail.Contains("Unable to load", StringComparison.OrdinalIgnoreCase) ||
        detail.Contains("DllNotFound", StringComparison.OrdinalIgnoreCase) ||
        detail.Contains("could not be found", StringComparison.OrdinalIgnoreCase);

    if (nativeProblem)
    {
        Console.WriteLine("NATIVE-FAIL: " + e.Message);
        return 1;
    }

    // Any other failure (e.g. "Steam is not running") means the native loaded.
    Console.WriteLine("NATIVE-OK: native loaded; Steam unavailable on CI (" + e.Message + ").");
    return 0;
}
