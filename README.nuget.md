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

You still need to be a Steamworks partner to ship a game on Steam. MIT licensed.
