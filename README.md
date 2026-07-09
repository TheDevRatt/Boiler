# Boiler

**Cross-platform Steam for Godot C# — without the boilerplate.**

If you've tried to wire Steam into a Godot C# game and had it work on Windows but
quietly die on your Mac or on a Linux build, you've met the problem Boiler solves.
Add the package, call `SteamClient.Init`, and Steam works — Windows, macOS
(Intel *and* Apple Silicon), and Linux. That's it.

```csharp
using Steamworks;

if (SteamClient.Init(480)) // 480 = Spacewar, Valve's public test app
{
    GD.Print($"Steam says hi to {SteamClient.Name}");
}
```

No manual DLL wrangling, no `runtimes/` folder archaeology, no "works on my
machine." Boiler installs itself when your game starts.

## Why this exists

Steam integration in .NET is two moving parts: a managed wrapper
([Facepunch.Steamworks](https://github.com/Facepunch/Facepunch.Steamworks)) and
Valve's native `steam_api` library. Getting them to load together inside Godot is
where everyone trips, for two reasons:

- **Godot ignores NuGet's native-loading convention.** The standard
  `runtimes/<rid>/native` mechanism that Just Works in a console app does *not*
  fire inside Godot's .NET host — so the native library never gets found, and
  `SteamClient.Init` throws a cryptic "DLL not found."
- **The two halves drift.** The managed wrapper and the native binary have to
  agree on exact Steam interface versions. Mix a wrapper from one release with a
  native from another and you get an equally cryptic "entry point not found" the
  first time you touch Friends or networking.

Boiler handles both. It ships a managed + native pair taken from a **single**
Facepunch release (so they can't drift), copies the right native for your
platform next to your build, and installs a `DllImportResolver` at startup that
actually finds it — in the editor and in exported builds alike.

## What's inside

| Platform | Managed wrapper | Native |
|---|---|---|
| Windows x64 | `Facepunch.Steamworks.Win64` | `steam_api64.dll` |
| macOS (x64 + arm64) | `Facepunch.Steamworks.Posix` | `libsteam_api.dylib` (universal) |
| Linux x64 | `Facepunch.Steamworks.Posix` | `libsteam_api.so` |

You write against the normal Facepunch API — Boiler just makes it load.

## Install

```
dotnet add package TheDevRatt.Steam.Boiler
```

Then use Facepunch as you normally would. A module initializer registers the
native resolver before your first line of game code runs, so there's genuinely
nothing to call. (If you want to force it early or be explicit, `SteamNative.Register()`
is public and idempotent.)

Drop a `steam_appid.txt` next to your executable — or at your project root while
developing — so `SteamClient.Init` can run without launching through the Steam
client.

## Status

Early days, but working. Verified end-to-end against a fresh Chickensoft
[GodotGame](https://github.com/chickensoft-games/GodotGame) template: Steam comes
online in the Godot editor on Apple Silicon with the two lines above and nothing
else.

Still on the list:

- publish to NuGet.org
- a CI matrix that runtime-tests Windows and Linux (macOS is proven; the other
  two are build- and packaging-verified)
- a macOS codesigning/notarization guide for the bundled dylib in shipped `.app`s

## Notes

- Boiler bundles Valve's redistributable Steam binaries, the same way Facepunch,
  Steamworks.NET, and GodotSteam do. You still need to be a Steamworks partner to
  ship a real game on Steam.
- The Facepunch wrapper is MIT-licensed; this package is too.

## License

MIT.
