# Boiler

Cross-platform Steam for Godot C#, minus the boilerplate. Add the package, call `SteamClient.Init`, and Steam works on Windows, macOS (Intel and Apple Silicon), and Linux, in the editor and in exports, with no manual native setup.

## Install

```
dotnet add package TheDevRatt.Steam.Boiler
```

## Use

```csharp
using Steamworks;

SteamClient.Init(480); // 480 = Spacewar, Valve's public test app
GD.Print($"Steam: {SteamClient.Name}");
```

Boiler installs its native resolver automatically at startup, so there is nothing to wire up. You just use the normal Facepunch.Steamworks API.

## What it solves

Godot ignores NuGet's `runtimes/` native-loading convention, so the Steam native library never loads and `SteamClient.Init` throws a "DLL not found." On top of that, the managed wrapper and the native binary must agree on exact Steam interface versions or you get an "entry point not found." Boiler ships a version-matched Facepunch managed and native pair, plus a resolver that loads the correct binary for each platform.

## Platforms

| Platform | Native |
| --- | --- |
| Windows x64 | steam_api64.dll |
| macOS (x64 + arm64) | libsteam_api.dylib |
| Linux x64 | libsteam_api.so |

You still need to be a Steamworks partner to ship a game on Steam. MIT licensed.
