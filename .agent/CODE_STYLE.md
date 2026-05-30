# CODE_STYLE — IUUT .NET 8 / WPF conventions

> **Tier 2 — Binding for code tasks.**
> When implementing code, follow this style. When you find code in violation of this style, fix it as part of your change with a `style-fix` note in the PR.

| | |
| --- | --- |
| **Document** | Code Style |
| **Version** | 1.0.0 |
| **Stack** | C# / .NET 8 / WPF / `System.Text.Json` |
| **Amendment rule** | `.agent/AMENDMENT_PROCESS.md` |

---

## §1. Solution layout (per master doc §17)

```
IcarusUltimateUtilityTool/
├── src/
│   ├── IUUT.Core/           # Parsers, mutators, validation, presets, services. Zero UI deps.
│   ├── IUUT.Catalog/        # Embedded D_* table JSON.
│   ├── IUUT.App/            # WPF shell.
│   └── IUUT.Cli/            # Optional headless CLI.
├── tests/
│   └── IUUT.Core.Tests/
├── catalogs/                # Generated JSON at build time.
├── fixtures/                # Anonymized save snippets for tests.
├── scripts/                 # Tooling (governance lint, hooks).
└── ...
```

Do not invent new top-level projects without amending master doc §17 first.

### `IUUT.Core` folder convention

```
IUUT.Core/
├── Abstractions/    # Cross-cutting seams: IClock, IGuidProvider (+ System* impls)
├── Io/              # Safe file I/O: SafeSaveWriter, BackupManager, IcarusJson
├── Models/          # POCOs mirroring save-file JSON
├── Parsers/         # JSON → Models
├── Serializers/     # Models → JSON
├── ProspectBlob/    # zlib + base64 + SHA-1 + Adler-32 + FProperty
├── Services/        # SaveDiscoveryService, HealthScanService, etc.
├── Presets/         # LazyMaxService, RecoveryService, CustomEditService
├── Validation/      # ValidationEngine
├── Catalog/         # Catalog loaders (reads embedded resources from IUUT.Catalog)
├── Exceptions/      # IUUT.Core.Exceptions.*Exception types
└── Logging/         # SafeFormatter + sanitization helpers (SECURITY_PROTOCOL §4)
```

`BackupManager` is described as a "service" in master doc §9.2; it lives in `Io/`
because its nature is file I/O. It is still registered/consumed as a service.

---

## §2. Naming

| Construct | Convention | Example |
| --- | --- | --- |
| Namespaces | `IUUT.<Project>.<Folder>` | `IUUT.Core.Parsers` |
| Classes | PascalCase | `ProfileParser` |
| Interfaces | `I` + PascalCase | `ISaveSession` |
| Public methods | PascalCase | `LoadProfile` |
| Async methods | PascalCase + `Async` suffix | `ResolvePersonaNameAsync` |
| Private fields | `_camelCase` | `_backupManager` |
| Constants | PascalCase | `DefaultBackupRoot` |
| Local variables | camelCase | `var profilePath = ...` |
| Test methods | `MethodName_Condition_ExpectedResult` | `ParseProfile_WithUnknownMetaRow_PreservesKey` |
| Test classes | `<ClassUnderTest>Tests` | `ProfileParserTests` |
| Fixtures | `<scenario>.json` | `profile-with-exotic-uranium.json` |
| Files | One public type per file; file name matches type | `ProfileParser.cs` |

Avoid abbreviations except: `IUUT`, `UI`, `IO`, `JSON`, `XML`, `GUID`, `SHA1`, `URL`, `UTF8`, `LE` / `BE` (endian).

---

## §3. Language features

- **Nullable reference types** enabled project-wide (`<Nullable>enable</Nullable>`).
- **File-scoped namespaces** (`namespace IUUT.Core.Parsers;`) — not nested braces.
- **`var`** for local declarations when the type is obvious from RHS; explicit types when not.
- **Pattern matching** (`is`, `switch` expressions) preferred over `as` + null check.
- **Records** for immutable value types — `record SteamProfileDisplay(string SteamId64, string? PersonaName, ...)`.
- **Primary constructors** acceptable for small classes (`class Foo(ILogger logger)`); prefer explicit ctors for classes with ≥ 3 dependencies.
- **`required` modifier** for properties that must be set at construction.
- **Collection expressions** (`[1, 2, 3]`) acceptable; do not retrofit existing `new List<int> { ... }` unless touching the code for another reason.
- **`async/await`** all the way through — no `.Result`, no `.Wait()`, no `.GetAwaiter().GetResult()` in production code.
- **`CancellationToken`** parameter on every async public method that could be long-running (catalog refresh, Steam API call, batch backup).

---

## §4. Error handling

- **No silent catches.** `catch (Exception) { }` is banned. If you must swallow, log + comment why (and the comment must reference the CONSTITUTION article authorizing the swallow).
- **Throw specific exceptions.** Use the BCL hierarchy (`ArgumentException`, `InvalidOperationException`, `FileNotFoundException`, `JsonException`, etc.) before introducing custom exceptions.
- **Custom exceptions** in `IUUT.Core.Exceptions` namespace, named `<Specific>Exception`, derive from `InvalidOperationException` or `IOException` as appropriate.
- **Failures in save-file mutators** propagate with full context (file path, operation, underlying exception). Per CONSTITUTION III, the mutator restores from backup before re-throwing.
- **Validation failures** in `ValidationEngine` return a structured result (`ValidationResult { bool Ok; IReadOnlyList<Issue> Issues; }`), not exceptions.

---

## §5. JSON handling

- **`System.Text.Json`** is the only JSON library. No Newtonsoft.
- **Source-generated serializers** for hot-path types (`[JsonSerializable(typeof(ProfileModel))]`) when AOT becomes relevant; until then plain reflection-based is fine.
- **Extension data** (`[JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }`) on every model that mirrors a save-file shape — non-negotiable per CONSTITUTION VI.
- **Indentation** on serialize: tabs to match game output. (Custom `JsonSerializerOptions` with a `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` to allow `&` and friends in character names.)
- **UTF-8 without BOM** on every write. Use `JsonSerializer.SerializeAsync(stream, ...)` with the stream opened via `FileStream` (no BOM written). Never `File.WriteAllText` with the default encoding.
- **Nested-stringified blobs** (`Characters.json`, `AssociatedProspects_Slot_N.json`) handled with a `NestedStringifiedConverter<T>` — never re-implement the pattern per file.

---

## §6. Logging

- **`ILogger<T>`** via DI. No `Console.WriteLine` in `IUUT.Core`.
- **Log levels:** `Trace` (entry/exit), `Debug` (decision points), `Information` (user-visible operations), `Warning` (recoverable issues), `Error` (failures), `Critical` (data corruption risk).
- **No PII.** No SteamID64, no character name, no full file path in log messages. Use IDs hashed/redacted in `SECURITY_PROTOCOL.md` §4 form.
- **Structured logging** via message templates: `_logger.LogInformation("Loaded profile {ProfileId} with {CharacterCount} characters", redactedId, count);`.

---

## §7. WPF / UI

- **MVVM** strictly. ViewModels in `IUUT.App/ViewModels`, never in `IUUT.Core`.
- **`CommunityToolkit.Mvvm`** for `[ObservableProperty]` and `[RelayCommand]` source generators.
- **No code-behind business logic.** Code-behind contains only InitializeComponent and trivial event wiring.
- **Data binding errors as errors** — `<Project>` MSBuild property `<TreatBindingErrorsAsErrors>true</TreatBindingErrorsAsErrors>` (if not yet supported, equivalent test harness check).
- **Resource dictionaries** for styles in `App.xaml`; no inline styles.
- **DPI awareness** declared at app manifest level (`PerMonitorV2`).
- **No SystemColors hardcoded** — themeable via resources.

---

## §8. Async / threading

- **UI updates** marshal to the dispatcher via `Application.Current.Dispatcher.InvokeAsync` only when truly needed; prefer `ConfigureAwait(false)` in `IUUT.Core` and let the UI layer marshal at the boundary.
- **`ConfigureAwait(false)`** on every `await` in `IUUT.Core` and `IUUT.Catalog`.
- **No `Task.Run` in `IUUT.Core`.** That's the caller's choice.
- **Cancellation** plumbed through; do not catch `OperationCanceledException` to "be nice."

---

## §9. Determinism

- **No `DateTime.Now` / `DateTime.UtcNow`** at point of use — inject `IClock`. Backup filenames need timestamps; tests pin them.
- **No `Guid.NewGuid()`** at point of use for fixture-relevant code — inject `IGuidProvider`. (Production code that genuinely needs randomness obviously uses the real one.)
- **No `Random`** without seed when test-relevant.

---

## §10. File I/O

- **Paths** built via `Path.Combine` or `Path.Join`. No string concatenation, no `\` literals embedded in path strings except inside `[Environment.SpecialFolder]` resolutions.
- **`Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`** for save root; never hardcode `C:\Users\<name>\AppData\Local`.
- **Atomic writes** for save files: write to `<File>.iuut-tmp-<guid>`, fsync, rename to `<File>`. Backup the original to `<File>.iuut-backup-<ts>` before rename. (Per CONSTITUTION III.)
- **File handle lifetime** scoped with `using` / `await using`. No global static streams.

---

## §11. Tests

- **xUnit** test framework.
- **FluentAssertions** for readable assertions.
- **One concept per test.** A test failure should immediately tell you what behavior broke.
- **Arrange / Act / Assert** comments not required, but a blank line between each section helps.
- **Test data** in `fixtures/` or inline; never read from `%LOCALAPPDATA%` in tests — that's not deterministic.

---

## §12. Source control hygiene

- **`.gitignore`** keeps `/bin/`, `/obj/`, `*.user`, `.vs/`, `TestResults/`, `*.iuut-backup-*` out of the tree.
- **`.gitattributes`** sets `*.cs text eol=lf` (consistent EOL despite Windows-only target) and `*.json text eol=lf` for source JSON. Save-file outputs are CRLF in production (per game convention) — that's runtime, not source.
- **Line endings** at edit time: LF in source, CRLF when writing save-file outputs.

---

## §13. Comments

- **XML doc comments** on public API. Summary line is one short sentence; remarks fill in the why.
- **Inline comments** explain the why, not the what. "Reads byte 17 of the SteamID" is noise; "Length prefix includes NUL per UE FString semantics — see Icarus-Analysis §9.1" is signal.
- **`// TODO:`** must include the agent and a tracking issue: `// TODO(claude): refactor when catalog v2 lands — #42`.
- **No commented-out code** in committed source. Either keep it via a feature flag or delete it.

---

## §14. Style enforcement

- **`.editorconfig`** at repo root pins indent (4 spaces in C#, tabs in JSON), `dotnet_diagnostic.*` severity, `csharp_prefer_*` rules.
- **`dotnet format`** must produce zero changes before commit. (CI runs `dotnet format --verify-no-changes`.)
- **Analyzers** as errors for the rules pinned in `.editorconfig`; as warnings for the rest.

---

## Revision history

| Version | Date | Change |
| --- | --- | --- |
| 1.1.0 | 2026-05-25 | §1 `IUUT.Core` folder convention extended for WP-1: added `Abstractions/` (IClock, IGuidProvider) and `Io/` (SafeSaveWriter, BackupManager, IcarusJson); listed the already-documented `Exceptions/` and `Logging/` folders. Noted BackupManager lives in `Io/`. |
| 1.0.0 | 2026-05-25 | Adopted. .NET 8 / WPF / STJ conventions established. |
