# IUUT.App

WPF desktop shell.

**Authority:** `docs/IUUT-PROJECT-DOCUMENTATION.md` §6.1, §9.1, §10; `.agent/CODE_STYLE.md` §7.

## Folder layout (per CODE_STYLE §1, §7)

| Folder | Contents |
| --- | --- |
| `Views/` | XAML windows / user controls. Code-behind contains only `InitializeComponent` + trivial event wiring. |
| `ViewModels/` | `[ObservableProperty]` and `[RelayCommand]` via `CommunityToolkit.Mvvm`. ZERO domain logic — call into `IUUT.Core` services. |
| `Controls/` | Reusable `UserControl`s (orbital-stash card, talent-tree node, etc.). |

## Hard rules (summary)

- **MVVM strictly.** ViewModels in `ViewModels/`; never `IUUT.Core`.
- **No code-behind business logic.** Code-behind = `InitializeComponent` only.
- **Resource dictionaries** for styles in `App.xaml`; no inline styles.
- **DPI awareness** declared in `app.manifest` (`PerMonitorV2`).
- **Bindings-as-errors** at build (set via `Directory.Build.props` once the harness is in place).
- **No PII in design-time data** — use `TestUser`, `Char1`, etc.

## Single-file publish

```powershell
dotnet publish src/IUUT.App/IUUT.App.csproj `
  -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Produces `IUUT.exe`. Target size 15–25 MB per master doc §6.1.
