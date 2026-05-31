# UI DESIGN CONCEPT — "Glass Console"

> The visual language for IUUT: a frosted-glass, dark sci-fi console. This is the
> design-system companion to master doc **§10 (UX design)** — §10 owns *what each
> screen does*; this owns *how it looks and how that look is built in WPF*. Synthesized
> from the owner's design brief (2026-05-31).

| | |
| --- | --- |
| **Aesthetic** | Frosted glass terminals on a darkened ship console — "industrial spacecraft," not "soft mobile OS" |
| **Mode** | Dark only (v1). Light theme is out of scope. |
| **Authority** | Master §10; CODE_STYLE §7 (MVVM); this doc owns tokens + chrome |
| **Status** | Concept — pending implementation-route decision, then a scheduled WP |

---

## 1. Design principles (the non-negotiables)

1. **Glass is a material, not a coat of paint.** Frost only the *large* surfaces — the
   window backdrop's mid-layer: cards, side nav, dialogs, the info panel. Buttons, text,
   icons, and data are **solid and high-contrast** on top. If everything is glass, nothing
   reads.
2. **Three layers, always.** `L0` rich-but-low-frequency background → `L1` glass surfaces →
   `L2` crisp foreground. Contrast lives in L2; mood lives in L0; depth lives in L1.
3. **Two accents, used like instrumentation.** **Amber** = primary action / Icarus identity
   / warnings. **Ion-cyan** = selection, keylines, data, links. Never use both to mean the
   same thing. Accents are for *edges, lights, and small emphasis* — not for body text.
4. **Sharp, not bubbly.** Small radii (4px), thin keylines, chamfer/notch details. No big
   pill-rounded neumorphism except the deliberate "pill toggle" control.
5. **Legible at 1080p → 4K.** Body text is near-white on darkened glass at ≥ 4.5:1 contrast.
   Numbers (data grids) sit on a *darker* backdrop than decorative glass so digits stay crisp.

---

## 2. Color tokens

Dark "orbit" palette. Hex is the source of truth; WPF `Color`/`Brush` resources mirror these.

### Background (L0)
| Token | Hex | Use |
| --- | --- | --- |
| `space.void` | `#060A12` | deepest base, gradient outer stop |
| `space.deep` | `#0A1020` | mid gradient |
| `space.navy` | `#101A30` | gradient inner / top glow base |
| `glow.amber` | `#FF9E2C` @ ~8% | faint lower-right instrument glow |
| `glow.ion` | `#21B6E6` @ ~8% | faint upper-left instrument glow |
| `starfield` | `#FFFFFF` @ 3–6% | sparse 1px points + low-freq noise |

### Glass surfaces (L1) — tints over the blurred backdrop
| Token | Value | Use |
| --- | --- | --- |
| `glass.fill` | `#FFFFFF` @ 5.5% | default card/panel fill |
| `glass.fill.hover` | `#FFFFFF` @ 8% | hover |
| `glass.fill.active` | `#FFFFFF` @ 10% | pressed / selected surface |
| `glass.stroke` | `#FFFFFF` @ 14% | 1px outer border |
| `glass.highlight` | `#FFFFFF` @ 22% | 1px **top** inner highlight (sells the edge) |
| `glass.shadow` | `#000000` @ 45% | soft outer drop shadow for float |
| `data.backdrop` | `#0B0F18` @ 70% | darker panel behind data grids/numbers |

### Accents (L2)
| Token | Hex | Use |
| --- | --- | --- |
| `amber.core` | `#FFB24C` | primary action, Lazy Max, focus on warm controls |
| `amber.glow` | `#FF9E2C` | hover glow / keyline for amber |
| `ion.core` | `#46D6F2` | selection, keylines, graphs, links, info |
| `ion.glow` | `#21B6E6` | hover glow / focus ring for ion |

### Text + semantic (L2)
| Token | Hex | Use |
| --- | --- | --- |
| `text.hi` | `#EAF1FB` | primary text on glass (≥ 4.5:1) |
| `text.mid` | `#97A6BF` | secondary / metadata |
| `text.low` | `#5C6B85` | disabled / hint |
| `state.ok` | `#46E0A0` | safe — game not running, JSON OK |
| `state.warn` | `#FFC24C` | warning — confirm to proceed |
| `state.danger` | `#FF6E7C` | hard error / blocked |

---

## 3. Typography

| Role | Family (recommended) | Size / weight | Notes |
| --- | --- | --- | --- |
| Display / wordmark | **Embedded sci-fi display** (Orbitron / Saira / Rajdhani — all SIL OFL, embeddable) | 22–28, 600 | **Sparingly** — window title, screen titles only |
| Heading | Segoe UI Semibold, **uppercase + 0.08em tracking** | 15–17, 600 | "instrumentation" feel with a system font (zero footprint) |
| Body | Segoe UI | 13–14, 400/500 | system → no footprint, ships everywhere |
| Numeric / data | Cascadia Mono → Consolas | 13, 400 | tabular figures for save counts, currencies, hashes |

**Footprint note (CONSTITUTION/SECURITY):** body + headings use **system fonts** (zero bytes,
no license risk). Only the *display* font is embedded, and any embedded font must pass the
SECURITY_PROTOCOL §8 license check (OFL fonts are fine, embeddable, redistributable). Decide
embed-vs-skip when we build; the UI must look correct with a Segoe UI fallback regardless.

---

## 4. Spacing, radius, motion

- **Spacing scale (4px base):** `4 · 8 · 12 · 16 · 24 · 32 · 48`. Card padding `20–24`.
- **Radius:** cards/panels `4px`; pill toggle `999px` (the one intentional round control);
  window corners `8px` (Win11) / `0` (Win10 maximized).
- **Borders:** 1px keylines only. Selection adds a 2px accent left-keyline, not a thick frame.
- **Motion (subtle, sci-fi calm):** background gradient drifts very slowly (~30–60s loop);
  hover transitions 120–160ms ease-out (border→accent, +glow, +2% fill); no bouncy easing.
  Honour reduced-motion: drop the drift animation if the OS requests it.

---

## 5. Glass recipe (the elevation system)

A glass surface = **five stacked layers**, back to front:

1. **Backdrop blur** of L0 behind the surface bounds.
2. **Tint fill** — `glass.fill` (raise on hover/active).
3. **Outer 1px border** — `glass.stroke`.
4. **Top inner highlight** — 1px gradient `glass.highlight → transparent` on the top edge.
5. **Outer drop shadow** — `glass.shadow`, large soft blur, low offset → "floats."

State deltas:
- **Hover:** fill → `glass.fill.hover`; border → accent @ 40%; add outer glow in the surface's
  accent (amber for action tiles, ion for selectable rows).
- **Selected:** 2px accent left-keyline + border → accent @ 70%.
- **Disabled:** fill 3%, text `text.low`, no glow.

> **Performance reality:** real backdrop blur is the expensive layer. Because L0 is
> *low-frequency* (a soft gradient), a translucent tint over it already reads as glass
> **without** a true blur. So: **translucent-tint glass everywhere by default; reserve a real
> `BlurEffect` for the hero Home tiles** (where the starfield detail benefits from frosting).
> This keeps 4K smooth. See §8 for the WPF mechanics.

---

## 6. Custom window chrome (frameless + self-drawn controls)

The window is **frameless with a custom title bar** — built with WPF's built-in
`System.Windows.Shell.WindowChrome` (no third-party dependency).

**Setup**
- `WindowStyle="None"`, `AllowsTransparency="False"` (we don't blur the *desktop* — the orbit
  background lives *inside* the window, so we keep hardware acceleration + crisp text).
- Attach `WindowChrome`: `CaptionHeight=40`, `ResizeBorderThickness=6`, `GlassFrameThickness=0`,
  `CornerRadius=0`, `UseAeroCaptionButtons=False`.
- The custom title bar (40px) draws: app glyph + subtle glow, wordmark, and right-aligned
  **min / max-restore / close** buttons.

**Behaviors**
- Drag-to-move comes free from `CaptionHeight`. Buttons opt **out** of caption dragging with
  `WindowChrome.IsHitTestVisibleInChrome="True"`.
- Buttons call `SystemCommands.MinimizeWindow/MaximizeWindow/RestoreWindow/CloseWindow`.
- **Maximize overflow fix** (WPF maximized windows clip by the resize border): pad the root by
  the maximized border (handle `StateChanged` → margin `8` when `Maximized`, else `0`), or hook
  `WM_GETMINMAXINFO`. Documented gotcha; bake it into the chrome control once.
- **Accessibility:** every control gets `AutomationProperties.Name`; Alt+Space still opens the
  system menu; close button is the only `state.danger`-hover control.
- **Optional later:** Win11 Snap Layouts (hover the maximize button) via `WM_NCHITTEST` →
  `HTMAXBUTTON`. Nice-to-have, not v1.

The chrome is a reusable control/style so every window (main + dialogs) shares it.

---

## 7. Component direction

| Component | Direction |
| --- | --- |
| **Title bar** | 40px glass strip, app glyph (amber glow), wordmark (display font), self-drawn window controls |
| **Mode tiles (Home)** | Large glass tiles, line-based sci-fi icon + title + one-line desc. Hover = blur up + accent outer glow. Lazy Max tile = amber; Recovery = ion; Custom = neutral→ion |
| **Buttons** | *Solid*, not glass. Primary = amber fill / dark text; secondary = ghost (glass.stroke border, text.hi); danger = red ghost → red fill on hover |
| **Pill toggle** (Lazy Max) | `999px` pill, faint neon keyline; off = `text.low` track; on = accent track + glow + knob |
| **ComboBox / lists** | Glass field; popup is `data.backdrop` (darker) so options stay crisp; selected row = ion left-keyline |
| **Data grid** (Custom/Stash) | Sits on `data.backdrop`, **not** decorative glass; mono numerals; zebra via 2% white; ion selection |
| **Status bar** | Thin holographic keyline (1px ion gradient) + inline **channel lights**: save-path · backup on/off · offline/online · game-state. Each = a small dot (`ok`/`warn`/`danger`) + tiny label |
| **Dialogs** (confirm Lazy Max) | Centered glass card over a dimmed scrim; lists the 4 files + counts (F-034); primary amber, cancel ghost |

---

## 8. Screen blueprints

- **Home** — three centered glass mode tiles; above them the info panel (save root + Browse,
  profile dropdown by PersonaName, health/game/steam channel lights); status bar below.
- **Broken Save Recovery** — tall glass save-list panel (left) + wider glass detail/actions
  panel (right); L0 drifts subtly behind.
- **Lazy Max** — one hero glass card with pill toggles (Talents · Currencies · Accolades ·
  Bestiary), then the confirm dialog (already wired, WP-14). *Note:* our current Lazy Max is
  one-click "max all four"; the per-category toggles are a Custom-mode/Lazy-Max-options
  evolution — the hero card can start as a single confirm and grow toggles later.
- **Custom Editor** — glass side-nav (the 15 categories, master §10.3) + main editor panel;
  data grids on `data.backdrop`.

---

## 9. WPF implementation routes

| Route | What | Pros | Cons |
| --- | --- | --- | --- |
| **A. Hand-rolled (recommended)** | Our own `ResourceDictionary` theme + built-in `WindowChrome` | Full control of the bespoke look; **minimal footprint** (no UI-lib MBs — aligns with the single-file no-install goal); license-clean; no framework lock-in | We author every control style (more upfront XAML) |
| **B. UI library** | WPF-UI / ModernWpf / MahApps (all MIT) | Polished controls + acrylic/Mica + theming out of the box; faster start | Adds a dependency (license review §8 + footprint); imposes Fluent's look — heavy overrides to hit *this* bespoke glass; risk of "library default" feel |
| **C. Hybrid** | Hand-rolled visuals + borrow only `WindowChrome` helpers / a Mica interop snippet | Control + a couple of solved hard bits | Small dependency surface to vet |

**Recommendation: Route A (hand-rolled).** The vision is bespoke (specific palette, glass
recipe, custom chrome, channel-light status bar) — a library's defaults would be fought more
than used, and a single-file ~15-25 MB self-contained exe (master §6.4) wants the smaller
footprint. `WindowChrome` (built-in) already solves the frameless window. We keep total control
and zero new license obligations.

**Proposed resource structure** (`src/IUUT.App/Theme/`):
```
Theme/
  Colors.xaml        // Color + SolidColorBrush tokens (§2)
  Typography.xaml    // text styles (§3)
  Glass.xaml         // the glass Style/ControlTemplate bits (§5)
  Controls.xaml      // Button/ToggleButton/ComboBox/etc. styles (§7)
  Window.xaml        // WindowChrome + title-bar control (§6)
  Theme.xaml         // merges the above; referenced from App.xaml
```
Background L0 = a `Grid` with a `RadialGradientBrush` + a tiled star/noise layer + the two
faint accent glows; cards layered over it. Real blur (hero tiles) = a `VisualBrush` of L0 with
`BlurEffect` clipped to the tile, or a pre-rendered frosted asset. Everything else =
translucent tint (cheap, smooth at 4K).

---

## 10. Accessibility & legibility guardrails

- Body text ≥ **4.5:1** contrast on its glass; large text ≥ 3:1. Accents (amber/ion) are for
  keylines/icons/large emphasis — **not** small body text on dark (they can fail AA).
- Don't encode state by **colour alone**: channel lights pair a dot with a text label; the
  game-state banner has words, not just red/green.
- Min hit target 32×32 (window controls 40-tall). Visible keyboard focus ring (ion).
- Honour OS reduced-motion (kill the gradient drift). Respect OS text-scaling.
- The look must degrade gracefully: with blur disabled or on a weak GPU it's still legible
  (translucent tint + borders carry it).

---

## 11. How it lands without redoing Core

The whole concept is **L2-and-below presentation**: it touches only `IUUT.App` (XAML + a
chrome control + the background). `IUUT.Core` (services, validation, apply pipeline) and the
ViewModels are untouched — they already hold all logic (CODE_STYLE §7). Phasing:

1. **Foundation** — `WindowChrome` title bar + `Theme/*.xaml` tokens + L0 background. (Re-skins
   the existing provisional `MainWindow` from WP-13; no VM change.)
2. **Home** — restyle the info panel + three glass mode tiles + channel-light status bar.
3. **Propagate** — apply the styles to each screen as its feature WP lands (Recovery, Custom…),
   so new screens are born styled rather than restyled later.

This replaces the old "UI is all Phase 6" plan: the *design system* lands as a scheduled WP now,
and each feature screen adopts it as it's built.
