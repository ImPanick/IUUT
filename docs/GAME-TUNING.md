# GAME TUNING — Engine.ini console-variable presets ("Engine Mods")

> The detailed spec for IUUT's future **Game Tuning** feature: on/off toggle cards that write
> publicly-known Unreal Engine console variables into the game's `Engine.ini` to tune
> performance and visuals (the headline being **"Buff FPS"**). Expands master doc **§20.1**.
> **Documentation only — post-v1.0, not yet built.** The phasing plan is in §8.

| | |
| --- | --- |
| **Target file** | `%LOCALAPPDATA%\Icarus\Saved\Config\WindowsNoEditor\Engine.ini` (UE4 client config — field guide §7.3) |
| **Engine** | Icarus is **Unreal Engine 4**. These are standard/universal UE cvars; **Icarus support is per-cvar and MUST be verified** against the live client (§7) — some cvars are read-only, scalability-locked, or ignored. |
| **Scope** | **Single-player / solo first.** Online/dedicated play may ignore client-side ini changes; never ship a toggle that could read as an unfair multiplayer advantage or trip anti-cheat (§7). |
| **Status** | Spec only. Out of v1 scope (NG3). Tracked as **Phase 7** (§8). |

---

## 1. Intent

A small set of **vetted, reversible** toggle cards — not a raw cvar firehose — that a player
flips to trade visual fidelity for frame-rate (or vice-versa), or to kill specific effects
(fog, volumetric clouds, motion blur). Each card owns a fragment of `Engine.ini`; turning it on
merges the fragment, turning it off removes exactly those lines. Everything is backed up and
reversible (§6). This is QoL/performance tuning with **public UE knobs**, not a cheat.

## 2. Engine.ini mechanics

Cvars and engine settings live in named INI sections. The four we use:

| Section | Purpose | Example keys |
| --- | --- | --- |
| `[ConsoleVariables]` | Startup cvars applied early (the primary home for `r.*` / `sg.*` / `t.*`). `[SystemSettings]` is an older equivalent and also works. | `r.VolumetricFog=0`, `sg.ShadowQuality=2`, `t.MaxFPS=120` |
| `[/Script/Engine.Engine]` | Core engine settings — frame-rate smoothing/cap, fixed frame rate. | `bSmoothFrameRate=False`, `bUseFixedFrameRate=False` |
| `[/Script/Engine.NetDriver]` | Base net-driver settings (note: a game often uses a **subclass** — see net section). | `NetServerMaxTickRate=60` |
| `[/Script/Engine.Player]` | Client bandwidth ("configured speed"). | `ConfiguredInternetSpeed=104857600` |

**Format:** UE INI — `[Section]` headers, `Key=Value` lines, `;` comments. A cvar set in
`[ConsoleVariables]` is applied at startup; the same cvar can also be changed live via the
in-game console (if enabled), which is how we **verify** (§7) before adding it to the catalog.

> **Universal vs game-dependent.** `r.*`, `sg.*`, `t.MaxFPS` are engine-universal cvars that
> exist in every UE4 title. Whether Icarus *honors* a given one (vs. clamping/locking it) is
> game-dependent and must be confirmed by the live dump. Treat every recipe below as a
> **candidate** until verified.
>
> **Datamined source (since v1.4.0).** Many of the catalog's cvars + their defaults/ranges are
> now taken from the game's own **`Icarus/Config/SettingsSchema.json`** (the schema behind the
> in-game settings menu) — e.g. `r.MotionBlur.Scale`, `r.ContactShadows`, `grass.DisableDynamicShadows`,
> `r.Shadow.CSM.MaxCascades`, `r.Streaming.PoolSize` + `…LimitPoolSizeToVRAM`, `r.VolumetricCloud`,
> and the `r.RayTracing.*` family. These are grouped (Visual FX / Frame Rate / Resolution & Quality /
> Shadows / Textures & Streaming), and the niche ones (ray tracing, tessellation) sit in an
> **`ADVANCED · MAY NOT APPLY`** group flagged `Experimental` — they are still candidates pending §7.

## 3. The toggle model (recap of §20.1)

- **ON** → merge the card's fragment: create the `[section]` if absent, else append the card's
  keys under the existing section (cards share a section rather than duplicating it).
- **OFF** → remove **only** the lines that card owns; drop a section left empty.
- **Duplicate-cvar guard** → before writing, if a cvar key already appears (IUUT-authored or
  hand-edited), warn and de-duplicate to a single authoritative line (UE uses last-writer).
- **Reconstructable** → toggle state is derived from which fragments are present, so IUUT
  reflects manual edits.

## 4. Console-variable reference (candidates — verify per §7)

### 4.1 Fog & volumetrics — `[ConsoleVariables]`
| cvar | Effect | "Off / cheap" value |
| --- | --- | --- |
| `r.Fog` | Master fog toggle | `0` |
| `r.VolumetricFog` | Volumetric (god-ray) fog | `0` |
| `r.VolumetricFog.GridPixelSize` | Volumetric fog resolution (higher = cheaper/coarser) | `16` |
| `r.VolumetricCloud` | Volumetric clouds | `0` |
| `r.VolumetricRenderTarget` | Volumetric render-target path | `0` |
| `r.SkyAtmosphere` | Sky-atmosphere rendering | `0` (aggressive) |
| `r.LightShaftQuality` | Light shafts / god rays | `0` |

### 4.2 Scalability scalars — `[ConsoleVariables]` (`sg.*`, 0=Low · 1=Med · 2=High · 3=Epic · 4=Cinematic)
| cvar | Group |
| --- | --- |
| `sg.ViewDistanceQuality` | Draw distance |
| `sg.ShadowQuality` | Shadows |
| `sg.GlobalIlluminationQuality` | GI |
| `sg.ReflectionQuality` | Reflections |
| `sg.PostProcessQuality` | Post-processing |
| `sg.TextureQuality` | Textures |
| `sg.EffectsQuality` | Effects/particles |
| `sg.FoliageQuality` | Foliage |
| `sg.ShadingQuality` | Shading |
| `sg.AntiAliasingQuality` | Anti-aliasing |

### 4.3 Render quality / FPS-impacting — `[ConsoleVariables]` (`r.*`)
| cvar | Effect | Perf value |
| --- | --- | --- |
| `r.ScreenPercentage` | Internal render scale (<100 upscales) | `80` |
| `r.ViewDistanceScale` | Multiplier on draw distance | `0.7` |
| `r.Streaming.PoolSize` | Texture-streaming pool (MB) | `3000` |
| `r.Shadow.MaxResolution` | Max shadow map size | `1024` |
| `r.Shadow.CSM.MaxCascades` | Cascaded-shadow cascades | `2` |
| `r.MaxAnisotropy` | Anisotropic filtering | `4` |
| `r.DepthOfFieldQuality` | Depth of field | `0` |
| `r.MotionBlurQuality` | Motion blur | `0` |
| `r.BloomQuality` | Bloom | `1` |
| `r.SSR.Quality` | Screen-space reflections | `0` |
| `r.SSGI.Enable` | Screen-space GI | `0` |
| `r.AmbientOcclusionLevels` | SSAO levels | `0` |
| `r.DefaultFeature.MotionBlur` | Default motion blur on/off | `0` |
| `r.DefaultFeature.AntiAliasing` | AA method (0 off · 2 TAA) | `2` |

### 4.4 Frame rate & smoothing
`[ConsoleVariables]`:
| cvar | Effect | Value |
| --- | --- | --- |
| `t.MaxFPS` | Hard FPS cap (0 = uncapped) | `120` or `0` |
| `r.VSync` | Vertical sync | `0` |

`[/Script/Engine.Engine]`:
| key | Effect | Value |
| --- | --- | --- |
| `bSmoothFrameRate` | Engine frame-rate smoothing | `False` |
| `bUseFixedFrameRate` | Lock to a fixed frame rate | `False` |
| `MinSmoothedFrameRate` / `MaxSmoothedFrameRate` | Smoothing range (if smoothing on) | `22` / `240` |

### 4.5 Net tuning (multiplayer)
`[/Script/Engine.Player]`:
| key | Effect | Value |
| --- | --- | --- |
| `ConfiguredInternetSpeed` | Client bandwidth ceiling (bytes/s) | `104857600` |
| `ConfiguredLanSpeed` | LAN bandwidth ceiling | `104857600` |

`[/Script/Engine.NetDriver]` (and/or the game's actual driver subclass — see note):
| key | Effect | Value |
| --- | --- | --- |
| `NetServerMaxTickRate` | Server net tick rate | `60` |
| `MaxClientRate` / `MaxInternetClientRate` | Per-client rate ceiling | `104857600` |
| `InitialConnectTimeout` / `ConnectionTimeout` | Connection timeouts (s) | `120` / `120` |
| `KeepAliveTime` | Keep-alive interval (s) | `0.2` |

> **Net-driver caveat:** UE games usually run a `NetDriver` **subclass**, most often
> `[/Script/OnlineSubsystemUtils.IpNetDriver]`. Settings under the base
> `[/Script/Engine.NetDriver]` may be ignored if Icarus uses a subclass — the live dump /
> log must confirm which class name applies before any net card ships. Net tuning is
> **multiplayer-only** and must never confer an unfair advantage or trip anti-cheat.

## 5. Candidate toggle cards (the user-facing presets)

Each card = a named on/off toggle owning one fragment. Examples (final values set after §7):

- **⚡ Buff FPS — Balanced** — `r.ScreenPercentage=85`, `sg.ShadowQuality=2`, `sg.EffectsQuality=2`,
  `r.MotionBlurQuality=0`, `r.DepthOfFieldQuality=0`, `r.VolumetricFog=0`.
- **⚡ Buff FPS — Aggressive** — the Balanced set plus `r.ScreenPercentage=75`,
  `r.ViewDistanceScale=0.6`, `sg.ShadowQuality=1`, `sg.FoliageQuality=1`, `r.VolumetricCloud=0`,
  `r.SSR.Quality=0`, `r.SkyAtmosphere=0`.
- **🌫 Disable Fog** — `r.Fog=0`, `r.VolumetricFog=0`.
- **☁ Disable Volumetric Clouds** — `r.VolumetricCloud=0`, `r.VolumetricRenderTarget=0`.
- **🎥 Disable Motion Blur** — `r.MotionBlurQuality=0`, `r.DefaultFeature.MotionBlur=0`.
- **🔭 Disable Depth of Field** — `r.DepthOfFieldQuality=0`.
- **🚀 Uncap FPS** — `t.MaxFPS=0`, `r.VSync=0`, `[/Script/Engine.Engine] bSmoothFrameRate=False`.
- **📡 Multiplayer Net Boost** — the `[/Script/Engine.Player]` + net-driver rate block (4.5),
  multiplayer-only, off by default.

Cards stay small and composable; multiple cards merge into shared sections (§3).

## 6. Safety

`Engine.ini` is **not** a save file, but a malformed INI can stop the game from launching —
so Game Tuning gets its own backup + validation flow that reuses the save-write discipline
(CONSTITUTION III; master §13.3): **timestamped backup → write temp → re-read/parse → atomic
rename**, with rollback on failure. An **INI** reader/writer (not the JSON `SafeSaveWriter`)
handles the format; the backup/atomic/rollback contract is identical. Toggling off is fully
reversible (owned-line removal). A "Reset Game Tuning" action restores the pre-IUUT backup.

## 7. Validation against the live client (no guessing)

We **do not ship a cvar as a vetted toggle until it's confirmed to take effect in Icarus.**
Process:

1. **Seed** the candidate list from public UE knowledge (§4).
2. **Dump** the live client's cvars on the operator's own machine — a user-runnable helper
   (`scripts/dump-cvars.ps1`, planned) drives the in-game console (`cvarList` / `DumpConsoleCommands`
   → log) and returns the dump. This is a **local, user-initiated** step — no network, no
   phone-home (CONSTITUTION V).
3. **Curate** the dump into the small vetted catalog; mark each cvar **Verified** (observed to
   change behaviour) / **Ignored** / **Locked**. Only Verified cvars become toggle cards.
4. **Re-verify on each game build** (a patch can lock or rename a cvar).

## 8. Phasing plan (when we build it — post-v1.0)

Mirrors the IMPLEMENTATION-PLAN style (Core-first, UI last). Tracked there as **Phase 7**.

| WP | Deliverable | Notes |
| --- | --- | --- |
| **GT-1** | `EngineIni` reader/writer — parse sections/keys, preserve unknown lines + comments, atomic backup-and-write (INI sibling of `SafeSaveWriter`) | §6; CONSTITUTION III |
| **GT-2** | Embedded **tuning catalog** (vetted cards → owned fragments) + `scripts/dump-cvars.ps1` helper | §4, §5, §7 |
| **GT-3** | **Toggle engine** — merge/remove owned fragments, shared-section append, duplicate-cvar de-dup, reconstruct state from file | §3 |
| **GT-4** | **INI validation** — lint the result (unknown/duplicate keys, malformed lines), warn-not-block; "Reset Game Tuning" restore | §6 |
| **GT-5** | **Game Tuning UI tab** — toggle cards in the Glass Console; lives with the parked UI pass | master §10, UI-DESIGN-CONCEPT |
| **GT-6** | **Manual validation** — run the dump on a live Icarus build, confirm each shipped card takes effect, mark Verified | §7 |

GT-1..GT-4 are testable Core (no game needed); GT-5 is UI (parked); GT-6 is operator-run.

## 9. Caveats & non-goals

- **Not a cheat / not a trainer.** Public render/perf cvars only; nothing that alters gameplay
  balance, and nothing for multiplayer that could be unfair or anti-cheat-sensitive.
- **No raw cvar editor in v1 of this feature** — vetted cards only (a raw/advanced mode is a
  later, clearly-labelled "expert" option, if ever).
- **Engine version drift** — UE4 vs UE5 cvar differences; re-verify per Icarus build (§7).
- **Per-cvar Icarus support is unconfirmed in this doc** — every value here is a candidate
  pending the live dump (§7). This document is the *plan*, not a guarantee any specific cvar
  is honoured by Icarus.
