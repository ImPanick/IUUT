# IUUT.Core

Domain library for IUUT. Zero UI dependencies.

**Authority:** `docs/IUUT-PROJECT-DOCUMENTATION.md` §6.2, §9.2; `.agent/CODE_STYLE.md` §1.

## Folder layout (per CODE_STYLE §1)

| Folder | Contents |
| --- | --- |
| `Models/` | POCOs mirroring save-file JSON. `[JsonExtensionData]` on every model per CONSTITUTION VI. |
| `Parsers/` | JSON → Models. Round-trip tested per TESTING_CONTRACT §2. |
| `Serializers/` | Models → JSON. UTF-8 no BOM. Tabs + CRLF for save-file outputs. |
| `ProspectBlob/` | Prospect blob codec: base64 + zlib (`ZLibStream`) + SHA-1 + Adler-32 + FProperty. See Icarus-Analysis §8.1. |
| `Services/` | `SaveDiscoveryService`, `SteamProfileResolverService`, `BackupManager`, `HealthScanService`, `GameProcessDetector`, etc. |
| `Presets/` | `LazyMaxService`, `RecoveryService`, `CustomEditService`. |
| `Validation/` | `ValidationEngine` + rules. Per master doc §13. |
| `Catalog/` | Catalog loaders. Reads embedded resources from `IUUT.Catalog`. |
| `Exceptions/` | `IUUT.Core.Exceptions.*Exception` types. |
| `Logging/` | `SafeFormatter` + sanitization helpers per SECURITY_PROTOCOL §4. |

## Hard rules (summary; full text in `.agent/CODE_STYLE.md`)

- **No UI references.** No `System.Windows`, no `Microsoft.UI`, no XAML.
- **No `Newtonsoft.Json`.** `System.Text.Json` only.
- **No `[System.Text.Encoding]::UTF8` or `Encoding.UTF8` for writes** — those emit a BOM. Use `new UTF8Encoding(false)`.
- **`ConfigureAwait(false)`** on every `await`.
- **Unknown JSON fields preserved on round-trip** (CONSTITUTION VI).
- **Backup → write → re-parse → restore-on-failure** for every save-file mutator (CONSTITUTION III).
- **No `DateTime.Now`** at point of use; inject `IClock`.
- **No `Guid.NewGuid()`** for fixture-relevant code; inject `IGuidProvider`.

This is a scaffold. Implementation lands per master doc §16 development phases.
