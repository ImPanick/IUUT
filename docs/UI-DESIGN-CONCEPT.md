# UI DESIGN CONCEPT — "Telemetry Console"

> The visual language for IUUT: a **SpaceX/military technical UI** — solid graphite panels,
> tight 1px grid lines, monospace-forward type, and a dense, high-signal telemetry aesthetic.
> This is the design-system companion to master doc **§10 (UX design)** — §10 owns *what each
> screen does*; this owns *how it looks and how that look is built in WPF*.
>
> **Supersedes** the v1 "Glass Console" concept (frosted glass + amber/ion glows + gradients).
> v1 is preserved in git history. Synthesized from the owner's revised brief (2026-05-31):
> *"solid Slate Graphite, grays/whites, 1px grid lines, minimal spacing, high-density telemetry —
> no blue→orange gradient shifts."* Reference mood: shaga.xyz (grid + solid + straight-to-the-point),
> pushed tighter/denser than the reference.

| | |
| --- | --- |
| **Aesthetic** | Instrument console / mission-control HUD — "graphite rack panel," not "frosted glass" and not "soft mobile OS" |
| **Mode** | Dark only. No light theme. |
| **Density** | Softened HUD — 2px radius, hard 1px borders, ~10px gutters (dense but comfortable; window sized up to ~1120×740) |
| **Type** | **Cascadia Code** (installed; ligatures) monospace-forward |
| **Authority** | Master §10; CODE_STYLE §7 (MVVM); this doc owns tokens + chrome |
| **Status** | Implemented — P1–P4 complete (see §11). Theme lives in `Theme/GlassTheme.xaml` + `Theme/Controls.xaml`. |
| **Visual ref** | `docs/mockups/telemetry-console.html` — approved interactive mockup; palette + density locked from it |

---

## 0. What changed from v1 (the inversion)

| Dimension | v1 Glass Console | **v2 Telemetry Console** |
| --- | --- | --- |
| Surfaces | Translucent white glass over a blurred backdrop | **Solid graphite panels** |
| Background | 2-stop blue gradient + drifting glows | **Flat charcoal** + optional faint 1px ruled grid |
| Depth | Soft outer drop shadows + accent glows | **1px borders + a hint of inner shadow**; no glow, no blur |
| Accent | Dual identity: amber + ion-cyan, used decoratively | **One action accent (SpaceX blue)**; color = state/action only |
| Corners | 6px | **2px** |
| Spacing | Loose, ad-hoc (12 different margin values) | **4/8 grid**, ~10px gutters, dense |
| Type | Segoe UI prose + display font + mono data | **Cascadia Code throughout**; Segoe UI only for long prose |
| Motion | Gradient drift, glow transitions | Minimal; instant or ≤120ms border-state changes |

The whole change is **L2-and-below presentation**: `IUUT.Core` and the ViewModels are untouched.

---

## 1. Design principles (the non-negotiables)

1. **Solid, not translucent.** No glass fills, no gradients on surfaces, no ambient glows. Flat
   graphite panels at two or three elevations, separated by 1px lines.
2. **Lines do the work, not gaps.** Structure comes from 1px borders and dividers, not whitespace.
   When in doubt, add a rule, not a margin. Dense is the goal.
3. **Monospace-forward and technical.** Numbers, paths, IDs, labels, and headers read like
   instrument readouts. Long descriptive prose may fall back to Segoe UI for legibility.
4. **Color is signal, not decoration.** The accent palette means *state or action* — blue =
   primary/interactive, green = ok, amber = warn, red = critical. Never paint a surface "to look nice."
5. **Legible and honest.** Body ≥ 4.5:1 contrast. State is never color-only (dot + label).
   The window must look correct on any GPU — there's no effect to fail back from.

---

## 2. Color tokens

Graphite "rack" palette. Hex is the source of truth; WPF `Color`/`Brush` resources mirror these.
**Implementation rule:** keep the existing `x:Key` names in `GlassTheme.xaml` and change only the
*values* — views already bind these keys, so P1 is a single-file edit with no view churn. The
"key" column lists the existing resource key to repoint.

**Palette: warm gunmetal** (locked from the HTML mockup — red channel a hair above blue so the
graphite reads warm, not cool). Wide steps between the three surface levels for clear elevation.

### Background & surfaces (L0/L1)
| Token | Hex | Existing key to repoint | Use |
| --- | --- | --- | --- |
| `bg.base` | `#131211` | `OrbitBackgroundBrush` (→ solid) | window background |
| `bg.recessed` | `#1A1816` | *(new)* `SurfaceRecessedBrush` | wells, scroll areas behind rows |
| `surface.raised` | `#232120` | `GlassCardBackground` (→ solid) | panels / cards |
| `surface.row` | `#2B2926` | `DataBackdropBrush` (→ solid) | data/setting rows, list items |
| `surface.row.hi` | `#353230` | *(new)* `SurfaceRowHoverBrush` | row hover / pressed |
| `grid.line` | `#3C3934` | `GlassStrokeBrush` (→ solid) | **default 1px border / divider** |
| `grid.line.hi` | `#4A463F` | *(new)* `GridLineStrongBrush` | emphasis border / hover edge |
| `edge.hi` | `#FFFAF4` @ 3.5% | *(new)* `EdgeHighlightBrush` | 1px milled top-edge inner highlight (panels/rows/tiles) |
| `fill.hover` | `#FFFFFF` @ 6% | `GlassFillHoverBrush` | subtle hover wash on interactive surfaces |

### Accents (state + action)
| Token | Hex | Existing key to repoint | Use |
| --- | --- | --- | --- |
| `accent.primary` | `#0A84E0` | `IonCoreColor` / `IonCoreBrush` | primary action, selection, links, focus ring, keylines |
| `state.ok` | `#4ADE80` | `StateOkColor` / `StateOkBrush` | safe — game not running, JSON ok |
| `state.warn` | `#F59E0B` | `StateWarnColor` + `AmberCoreColor` | warning — confirm to proceed; "Lazy Max backed up" |
| `state.danger`| `#EF4444` | `StateDangerColor` / `StateDangerBrush` | hard error / blocked / destructive |

> **Amber is now warning-only.** v1's amber *identity* (the Lazy Max brand glow) is retired —
> Lazy Max becomes a primary-blue action like the others, with amber reserved for its
> "backed up / caution" sub-label. This removes the two-accent decorative split.

### Text
| Token | Hex | Existing key | Use |
| --- | --- | --- | --- |
| `text.hi` | `#ECEAE7` | `TextHiColor` | primary text (warm white) |
| `text.mid` | `#9C988F` | `TextMidColor` | secondary / metadata |
| `text.low` | `#74706A` | `TextLowColor` | labels / hints (warm; clears AA — v1's `#5C6B85` failed) |

---

## 3. Typography

**Family: Cascadia Code** (installed system-wide; ligatures on). Cascadia Mono / Consolas are the
fallbacks. Monospace is the default for *all* UI text; Segoe UI is permitted only for multi-line
descriptive prose where mono hurts readability.

| Role | Family | Size / weight | Notes |
| --- | --- | --- | --- |
| Display / wordmark | Cascadia Code SemiBold | 18–20, 600 | window/screen titles; UPPERCASE optional |
| Section label | Cascadia Code SemiBold | 11, 600, UPPERCASE | the `LabelText` "readout caption" role |
| Body / control | Cascadia Code | 13, 400 | `BodyText` — buttons, list items, fields |
| Secondary | Cascadia Code | 12, 400 | `MutedText` — descriptions, hints |
| Numeric / data | Cascadia Code | 12–13, 400 | currencies, counts, hashes, paths (already mono) |
| Long prose (opt) | Segoe UI | 12–13, 400 | only where a paragraph reads poorly in mono |

**WPF constraint — letter-spacing:** the spec's `-0.02em` tracking is **not natively supported**
on `TextBlock` (WPF has no `LetterSpacing`). At 12–13px the effect is marginal — **drop it.**
(If ever required, it needs per-glyph runs or a custom typography pass; not worth it.)

**Fonts decision:** Cascadia Code is installed everywhere modern Windows ships it, but to be safe
for end users on older builds, **bundle `CascadiaCode.ttf`** with the app and reference via pack
URI (`/Fonts/#Cascadia Code`) so the look is self-contained (master §6.4 single-file goal). It
must still degrade to Consolas gracefully.

---

## 4. Spacing, radius, borders, motion

- **Spacing scale (4px base):** `4 · 8 · 10 · 12 · 16 · 24`. Gutters between panels **10px**. Panel
  padding **12–16**. Row padding **8–10 vertical / 12–14 horizontal**. Retire the v1 grab-bag of
  4/5/6/7/18/22 values — every margin must be on the scale.
- **Radius:** panels/cards/rows/controls **2px**. No pill/999px controls (the v1 neon toggle is
  gone — toggles are square-ish checkboxes or 2px segmented controls).
- **Borders:** **1px `grid.line` everywhere** — panels, rows, fields, dividers. Selection/active =
  border → `accent.primary` (or a 2px accent **left** keyline on list rows). Hover = border →
  `grid.line.hi`.
- **Depth:** no outer drop shadow, no glow. 1px **top inner highlight** (`edge.hi`, warm white
  ~3.5%) on raised panels/rows/tiles to suggest a milled edge; optional 1px bottom inner shadow at
  ~30% black. Subtle — this is the only "depth" cue. (In WPF: `box-shadow:inset` → a 2-border
  sandwich, since `DropShadowEffect` is outer-only.)
- **Optional grid backdrop:** a faint tiled 1px rule grid (`grid.line` @ ~30%) behind the content
  region sells the "HUD." Implement as a tiled `DrawingBrush` on the window background. Off by
  default until P3; must never reduce text contrast.
- **Motion:** minimal. Border/fill state changes ≤120ms or instant. No drift, no bounce. Honour
  OS reduced-motion (there's little to disable, which is the point).

---

## 5. Surface recipe (the elevation system)

A panel = **three flat layers**, back to front (no blur, no gradient):

1. **Solid fill** — `surface.raised` (`#1A1D26`); rows use `surface.row` (`#1F2430`).
2. **1px border** — `grid.line` (`#2A2F3A`).
3. *(optional)* **1px top inner highlight** — white @ 4% on the top edge only.

State deltas:
- **Hover (interactive):** fill → `surface.row.hi` / + `fill.hover` wash; border → `grid.line.hi`.
- **Selected/active:** border → `accent.primary`, or 2px `accent.primary` left keyline on rows.
- **Disabled:** opacity 0.45, text → `text.low`, no border emphasis.

Elevation is communicated by **fill step + a dividing line**, not shadow. Three steps max:
`bg.base` → `surface.raised` → `surface.row`.

---

## 6. Window chrome

Current implementation uses **WPF-UI `FluentWindow`** + `ui:TitleBar` (frameless, rounded). That
stays for v2 — but the title bar is restyled to the graphite palette (charcoal strip, mono
wordmark, `state.danger` only on close-button hover). The v1 "self-rolled `WindowChrome`"
recommendation is **not** pursued; we keep the existing WPF-UI chrome and skin it.

- Title strip: `bg.base`, 1px bottom `grid.line`, wordmark in Cascadia Code, app glyph as a
  monochrome line icon (no emoji, no glow).
- Window corners follow Win11 (`WindowCornerPreference="Round"`); content corners are 2px.
- Accessibility: every control keeps `AutomationProperties.Name`; visible focus ring in
  `accent.primary`; min hit target 32×32, title-bar buttons taller.

---

## 7. Component direction

| Component | v2 direction |
| --- | --- |
| **Iconography** | **No emoji.** Monochrome line icons via `ui:SymbolIcon` (Fluent System Icons), tinted `text.mid` (or `accent.primary` when active). This is the single biggest "de-slop" lever. |
| **Mode tiles (Home)** | Flat `surface.raised` panels, 1px border, line icon + mono title + one-line desc + a small "OPEN" affordance. Hover = border → `accent.primary`, fill → `surface.row.hi`. All four identical treatment (no special amber tile). Consider a denser 4-up row or 2×2 with a hero. |
| **Buttons** | Sharp (2px). Primary = `accent.primary` fill / `#0D0F14` text; secondary = ghost (1px `grid.line` border, `text.hi`); danger = red ghost → red fill on hover. No rounded Fluent pill. |
| **ComboBox / lists** | Flat field, 1px border; popup on `bg.recessed`; selected row = `accent.primary` left keyline; hover = `surface.row.hi`. |
| **CheckBox / toggles** | Square 2px box, 1px border; checked = `accent.primary` fill + check. **Whole row clickable**, not just the box. |
| **Slider** | Thin (2px) track on `grid.line`; filled portion `accent.primary`; small square thumb. |
| **Data / setting rows** | The canonical dense unit: `surface.row`, 1px border, `label + mono sub-row` left, control right, **aligned on a shared right column** so controls stack vertically. This is the shared `EditorRow` already emerging in `GameTunerView` / `AccountEditorView`. |
| **Status bar** | 1px top `grid.line` (flat, not gradient) + **channel lights**: a small dot (`ok`/`warn`/`danger`) + mono label per channel (save-path · backups · game-state). Right-aligned mono status readout. Keep this — it's the strongest existing instrument cue. |
| **Empty states** | Centered `text.mid` heading + `text.low` hint. (Added to Recovery in P0.) Never leave a panel blank. |
| **Headers** | Convert to readout style: `SECTION LABEL` (uppercase mono `LabelText`) over title; show live context (path, profile, counts) in mono. |

---

## 8. Screen blueprints

- **Home** — info panel (save root + Browse/Reload, profile combo, channel lights) as a compact
  readout block; below it the four mode tiles in a tight grid; flat status bar. Fill dead vertical
  space with *information* (save size, last backup, char count) rather than padding.
- **Broken Save Recovery** — header readout (title + path) → profile + Scan → recovery-plan panel
  (mono action lines on `surface.row`, advisories, empty state) → status + Repair.
- **Game Tuner** — header readout → **grouped** setting rows (Visual FX / Performance / Frame
  Rate) with a top **Presets** row (Max FPS / Balanced / Quality); controls right-aligned on a
  shared column; status + Apply. Today it's a flat ungrouped list — group it in P3.
- **Custom Editor** — profile selector → category side-nav (line icons, 1px divider) + editor
  panel via `CurrentEditor` (already wired: `AccountEditorView` real rows, `PlaceholderEditorView`
  for un-wired categories). New editors reuse the shared `EditorRow` template.

---

## 9. WPF implementation notes (constraints & mechanics)

- **Tokens:** repoint values in `GlassTheme.xaml` per §2, keeping `x:Key`s. Add the few new keys
  (`SurfaceRecessedBrush`, `SurfaceRowHoverBrush`, `GridLineStrongBrush`). Delete the L0 glow
  rectangles in `MainWindow.xaml` and the `IonGlowBrush`/`AmberGlowBrush`/`*GlowEffect` resources.
- **Controls are the real work.** v2's sharp look means **overriding WPF-UI's Fluent control
  styles** (Button, ComboBox, CheckBox, TextBox, Slider, ScrollBar). This is ~60% of the effort
  and what separates "recolored" from "console." Author these in a new `Theme/Controls.xaml`
  merged after the WPF-UI dictionaries so our styles win.
- **Inner shadow** = a 2-border sandwich (outer 1px `grid.line`, inner 1px top-highlight), not a
  `DropShadowEffect` (which is outer-only).
- **Letter-spacing** — unsupported; dropped (see §3).
- **Grid backdrop** — tiled `DrawingBrush`, applied to the content `Grid` background, clipped to
  bounds; keep opacity low.
- **Font bundling** — add the TTF to the project as `Resource`, reference `pack://…/Fonts/#Cascadia
  Code`; verify Consolas fallback.

---

## 10. Accessibility & legibility guardrails

- Body ≥ **4.5:1** on its surface; large text ≥ 3:1. `text.low` bumped to `#6B7280` to clear AA
  for labels (v1's `#5C6B85` failed).
- State is **never color-only**: channel lights pair dot + label; the game-state cue has words.
- Min hit target 32×32; whole rows clickable where a row is the control. Visible
  `accent.primary` focus ring. Respect OS text-scaling.
- The look has no effect to fail back from — it's legible on any GPU by construction.

---

## 11. Phasing (how it lands)

Presentation-only; `IUUT.Core` + ViewModels untouched.

- **P0 — done.** Recovery header overlap fix; Lazy Max glow→hover; Recovery/empty states.
- **P1 — done.** `GlassTheme.xaml` repointed to the warm-gunmetal palette + 2px radius; glow
  layers removed from `MainWindow.xaml`; text styles → Cascadia Code; window sized to 1120×740.
  App went graphite with no view edits.
- **P2 — done.** `Theme/Controls.xaml` overrides WPF-UI's Fluent resource keys (accent / control
  fills / strokes / text / `ControlCornerRadius=2` / solid-background popups) → ComboBox / TextBox /
  CheckBox / Slider / ScrollBar recolour without re-templating. Added explicit `GhostButton`
  (implicit default), `PrimaryButton`, `DangerButton`. Title bar inherits via `ApplicationBackgroundBrush`.
- **P3 — done.** Game Tuner grouped (Visual FX / Frame Rate / Performance) via a presentation-only
  `Group` property + default-view `GroupDescription` + XAML `GroupStyle`; readout-style headers on
  Recovery / Game Tuner / Account; primary buttons on Repair / Apply×2; Home status line flattened
  (gradient → 1px `grid.line`); button labels uppercased; spacing normalised toward 8/10.
- **P4 — done.** All emoji removed from `IUUT.App` (verified by sweep — zero remain in XAML or C#).
  Home tiles + nav (line icons + `01–04` index); Custom category sidebar (`CustomCategory.Glyph` is now
  a typed `SymbolRegular`, rendered via `ui:SymbolIcon` in `CustomView` + `PlaceholderEditorView`); every
  `*EditorView`/`*ViewerView` header → readout label, apply/save/unstick → `PrimaryButton`, remove →
  `DangerButton`, emoji buttons → uppercase text; Recovery `ℹ`/`⚠` → `ui:SymbolIcon` (Info/Warning); the
  stash durability chip `🔧` → `DUR`; dialog/status emoji removed.

All 256 Core tests green after the UI overhaul; the App layer builds 0-error. (Category glyph mapping:
account→`WalletCreditCard24`, characters→`Person24`, accolades→`Trophy24`, stash→`Box24`,
loadouts→`Backpack24`, prospects→`Map24`, mounts→`AnimalPawPrint24`, flags→`Flag24`, raw→`Code24`.)
- **P4 — iconography**: replace every emoji with `ui:SymbolIcon` line icons.

Each feature screen adopts the styles as it lands, so new screens are born styled.
