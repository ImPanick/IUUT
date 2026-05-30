# IUUT.Core.Tests

xUnit + FluentAssertions test suite for `IUUT.Core`.

**Authority:** `.agent/TESTING_CONTRACT.md`; `.agent/CODE_STYLE.md` §11.

## Layout (per TESTING_CONTRACT §1)

| Folder | Contents |
| --- | --- |
| `Unit/` | One class / one method. No I/O. No DI graph. |
| `Integration/` | Parser → mutator → serializer round-trips against `fixtures/`. Backup → write → restore flows. |
| `Regression/` | Named after the originating bug / issue. Permanent guards. |
| `Snapshot/` | Outer-wrapper shape preservation (Characters.json, AssociatedProspects_*). |

## Naming

`MethodName_Condition_ExpectedResult`:

- `ParseProfile_WithUnknownMetaRow_PreservesKey`
- `WriteCharacters_OnReParseFailure_RestoresFromBackup`
- `EncodeProspectBlob_AppendsBigEndianAdler32Trailer`

Avoid `Test1`, `RoundTripTest`, `ItWorks`.

## Determinism

- No `DateTime.Now` — inject `IClock`.
- No `Guid.NewGuid()` for fixture-relevant code — inject `IGuidProvider`.
- No network I/O.
- No reading from `%LOCALAPPDATA%`.

## Running

```powershell
dotnet test tests/IUUT.Core.Tests/IUUT.Core.Tests.csproj --logger "console;verbosity=minimal"
```

## Coverage targets (per TESTING_CONTRACT §6)

- Parsers / serializers / mutators: ≥ 90% line coverage.
- Services / presets: ≥ 80%.
- Coverage is a tool, not the gate. The gate is per-change-type tests in TESTING_CONTRACT §2.
