# Spec: Cross-Platform Steam Native Loading for Godot C#

**Status:** Draft, core decisions resolved
**Author:** TheDevRatt
**Package:** `TheDevRatt.Steam.Boiler` ("Boiler"; personal namespace first, propose to Chickensoft later if it proves useful)
**Date:** 2026-07-09
**Reference implementation:** Eldritch Engine (`Sync/Steam/`, `Directory.Build.props`)

---

## 1. Summary

Getting Steam to work in a Godot 4 **C#** project is reliable on Windows and
broken-by-default everywhere else. The binaries and wrappers exist, but nothing
makes them *load correctly inside Godot* across platforms. This spec proposes a
small Chickensoft package that closes that gap: it delivers a **version-matched
Steam managed+native pairing** plus the **Godot-specific loader glue** that makes
the native library resolve in both the editor and exported builds, on Windows,
macOS (Intel + Apple Silicon), and Linux.

This is **not** a Steam API wrapper and **not** a multiplayer transport. It is the
plumbing layer that makes an existing wrapper (Facepunch.Steamworks) actually load
under Godot, cross-platform.

---

## 2. Background: the two-layer architecture

A Steam integration in .NET has two independently-versioned halves:

| Layer | What it is | Platform-specific? |
|---|---|---|
| **Managed** | The C# wrapper you call (`SteamClient.Init`, …). Thin P/Invoke shim. | Yes, Win64 vs Posix builds differ in the native name they call and in struct packing. |
| **Native** | Valve's C++ `steam_api` engine (`steam_api64.dll` / `libsteam_api.dylib` / `.so`). | Yes, one binary per OS/arch. |

The managed layer reaches the native layer through P/Invoke, where the native
library name is a **compile-time constant** and the required interface versions
(e.g. `SteamAPI_SteamFriends_v017`) are baked into the managed build.

---

## 3. Root causes (the two real problems)

### 3.1 Godot ignores NuGet's native-loading convention

NuGet ships per-platform natives under `runtimes/<rid>/native/`. A normal .NET app
resolves these automatically. **Godot does not**, in the case that matters most:

- **Editor / `dotnet build` (no RID):** `runtimes/` natives are never copied to
  the build output, so the file simply isn't where the game runs.
- **Load context:** even when present, Godot's host doesn't probe `runtimes/` the
  way `dotnet run` does.

Result: a Godot C# project that references a correct, cross-platform native
package still fails to load Steam. **A resolver + an editor-time copy are
required.** (Verified: the standard package alone does not load in the Godot 4.7
editor.)

### 3.2 Managed ↔ native version pairing is fragile and unguarded

The native must export the exact interface accessor versions the managed wrapper
P/Invokes. These drift between Steam SDK releases. Mixing a managed wrapper and a
native binary from different SDK generations fails at `SteamClient.Init` with
`entry point 'SteamAPI_Steam<Interface>_v0NN' not found`.

Observed concretely: the `Facepunch.Steamworks 2.5.2` managed wrapper needs
`SteamAPI_SteamFriends_v017`. The `Facepunch.Steamworks.Dll` **1.62** native ships
`v018` and breaks; **1.61** ships `v017` and works. No published native package
version is a *perfect* superset of the 2.5.2 wrapper's 32 accessors, 1.61 is the
closest and still lacks one unused accessor (`SteamAppList_v001`).

**Implication:** naively gluing "latest managed" to "latest native" is a coin
flip. The pairing must be *curated and pinned*.

---

## 4. Evidence (validated on real hardware)

On an Apple Silicon Mac, Godot 4.7 stable mono, arm64-native process:

- With the resolver + editor-copy and a **matched** native (1.61):
  `[SteamNetwork] Online as <user> (<id>)`, Steam initializes and authenticates.
- With a **mismatched** native (1.62): init fails on `SteamAPI_SteamFriends_v017`.
- The native (`libsteam_api.dylib`) loads only because the resolver points at it;
  the default probe does not find it in the editor build.
- 252/252 unit tests pass with Steam correctly dormant under `--run-tests`.

Both root causes are therefore demonstrated, not theorized, and both are solvable.

---

## 5. Prior art & the gap

| Package | Managed | Native | Cross-platform | Godot resolver | Verdict |
|---|---|---|---|---|---|
| `Facepunch.Steamworks` (NuGet) | Win64 only | loose Win dll | ❌ | ❌ | Windows-only |
| `Facepunch.Steamworks.Dll` | none | all platforms, `runtimes/` | ✅ (binaries) | ❌ | natives only; no glue; unpinned pairing |
| `Facepunch.Steamworks.Library` | none | Win only | ❌ | ❌ | Windows-only |
| `TheProjectPioneer.Godot.Steam` | Win64 | Win only | ❌ | ❌ (not needed on Win) | Godot MultiplayerPeer, Windows-only |
| `Steamworks.NET` | cross-platform | separate | partial | ❌ | different API; still no Godot glue |

**The gap:** no package delivers *cross-platform Steam that actually loads in
Godot C#*. The pieces exist; the Godot-specific loader glue + curated pairing do
not. This is the wedge.

---

## 6. Proposed solution

A Chickensoft package (working name **`Chickensoft.GodotSteam`** or
`Chickensoft.Steamworks.Godot`) that provides:

### 6.1 What's in the box

Built on **Facepunch.Steamworks**, sourcing **both halves from a single upstream
Facepunch GitHub release** and **bundling** them in the package:

1. **The managed wrapper**, both platform builds, `Facepunch.Steamworks.Win64.dll`
   and `Facepunch.Steamworks.Posix.dll`, taken from Facepunch release *X*.
2. **The native binaries** for every platform, `steam_api64.dll`,
   `libsteam_api.dylib` (universal), `libsteam_api.so`, taken from the **same**
   release *X*'s `redistributable_bin`.
3. **The Godot loader glue:**
   - An MSBuild `.targets` that selects the right managed assembly per target and,
     on a no-RID (editor) build, copies the host platform's native beside the
     built assemblies. (RID builds place it the same way.)
   - A `NativeLibrary.SetDllImportResolver` registered via a `[ModuleInitializer]`
     so the native resolves with **zero consumer code**.
4. **Docs** for the macOS export path (codesign / notarization of the bundled
   dylib), the one place a resolver can't help.

Because both halves come from the *same release*, they are built against the same
Steam SDK and their interface versions **cannot drift apart**, see §6.3.

### 6.2 Consumer experience

```
dotnet add package Chickensoft.GodotSteam
```

That is the entire opt-in. Build → correct native placed; run → resolver loads it;
Steam works in the editor and in Windows/macOS/Linux exports. No flags, no manual
DLL placement, no per-project MSBuild.

### 6.3 Why this shape

- **Single-release sourcing neutralizes root cause #2.** The version-pairing
  fragility (§3.2) exists *only* when the managed wrapper and native come from
  different origins, which is exactly the trap the reference impl hit (2.5.2
  managed + a separately-versioned native package → the 1.61/1.62 `SteamFriends`
  break). Taking both halves from one Facepunch release makes them matched **by
  construction**: they were compiled against the same SDK, so accessor drift is
  structurally impossible, not merely tested against. The §9 parity check becomes
  a belt-and-suspenders guard rather than the primary defense.
- **Bundling** guarantees offline, deterministic builds (no build-time network)
  and keeps the matched pair together. Valve's redistributables are shipped by
  every wrapper already (Facepunch, Steamworks.NET, GodotSteam); bundling is the
  community norm, and under a personal namespace it carries the same posture they
  do.
- **The resolver is the moat:** it is the one thing no existing package does
  cross-platform, and it is provably required (§4).

---

## 7. Non-goals

- **Not a Steam API wrapper.** It rides on Facepunch (or Steamworks.NET); it does
  not reimplement P/Invoke bindings or fight the SDK-version treadmill at the API
  level.
- **Not a multiplayer transport / `MultiplayerPeer`.** That is a separate,
  later project (Manifold). This package is the load layer Manifold would sit on.
- **Not a Godot project scaffolder.** Templates/`dotnet new` are orthogonal; they
  may reference this package but do not contain it.

---

## 8. Decisions

1. **Bundle the native binaries.** Shipped inside the package, offline,
   deterministic, no build-time network. (Accepts a ~2–3 MB package; fine.)
2. **Base wrapper: Facepunch.** The nicer API; the game code already targets it.
   Its managed wrapper isn't on NuGet cross-platform, which is *why* we bundle it
   (decision 1) rather than take a package dependency.
3. **Source both halves from one upstream Facepunch release.** The managed
   (`netstandard2.1` Win64 + Posix) and the native (`redistributable_bin` for
   win/osx/linux) both come from the *same* Facepunch GitHub release zip. This is
   the decision that makes the pairing correct by construction (§6.3), no more
   accessor-matching roulette between independently-versioned packages.
4. **Namespace: `TheDevRatt.*` first.** Incubate under the author's personal
   namespace/repo, matching Chickensoft conventions (package template, GoDotTest,
   CI) so adoption is a formality. Propose transfer to Chickensoft later, only if
   it proves useful.

### Open (deferred, non-blocking)

- Which specific Facepunch release to pin as *X* for the first cut (whichever is
  latest-stable with a universal arm64 macOS dylib; 2.5.2 is a known-good
  candidate).
- Whether to also expose a knob for consumers who ship their own Steam SDK build.

---

## 9. Validation plan

- CI matrix: build for `win-x64`, `linux-x64`, `osx` (universal). Assert the
  correct native lands in output and accessor parity holds (an automated version
  of the `nm`/`strings` diff used to catch the 1.62 break).
- A headless Godot smoke test per platform that boots, calls `SteamClient.Init`,
  and asserts either "online" (Steam present) or a clean "Steam unavailable"
  warning, never a `DllNotFound` / missing-entry-point.
- Editor (no-RID) and export (RID) both covered, since they exercise different
  native-placement paths.

---

## 10. Appendix: reference implementation

Eldritch Engine already implements every mechanism this package would generalize:

- `Directory.Build.props`, per-platform managed selection, `Facepunch.Steamworks.Dll`
  pinned to 1.61.0, `CopySteamNativeForEditor` target.
- `Sync/Steam/SteamNetwork.cs`, `SetDllImportResolver` + candidate-path search.
- `docs/steam-cross-platform.md`, the platform/version rationale and macOS signing.

It is, in effect, an unpackaged prototype of §6.
