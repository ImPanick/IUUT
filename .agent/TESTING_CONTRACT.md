# TESTING_CONTRACT — What must be tested, with what, how

> **Tier 2 — Binding for any code task.**
> The bar for a "tested" change in IUUT is higher than the language default because the cost of a save-corruption regression is a player's hundreds of hours.

| | |
| --- | --- |
| **Document** | Testing Contract |
| **Version** | 1.0.0 |
| **Authority** | CONSTITUTION III (Safety-first), VI (Forward compatibility) |
| **Amendment rule** | `.agent/AMENDMENT_PROCESS.md` |

---

## §1. Test layers

| Layer | What it covers | Where it lives |
| --- | --- | --- |
| **Unit** | One class / one method, no I/O, no DI graph | `tests/IUUT.Core.Tests/Unit/` |
| **Integration** | Parser → mutator → serializer round-trips against fixtures; backup → write → restore flows | `tests/IUUT.Core.Tests/Integration/` |
| **Regression** | Specific bug recurrences, named after the issue | `tests/IUUT.Core.Tests/Regression/` |
| **Snapshot** | Outer-wrapper shape preservation (Characters.json, AssociatedProspects_*) | `tests/IUUT.Core.Tests/Snapshot/` |
| **Manual** | UI smoke, game-load acceptance, prospect-blob mutation under real game | Documented checklist in `tests/MANUAL_CHECKLIST.md`; not executable in CI |

CI runs Unit + Integration + Regression + Snapshot. Manual runs are PR-gated by human reviewer; the reviewer ticks the manual checklist items in the PR review.

---

## §2. Mandatory tests per change type

### Parser added or modified

- **Round-trip** test: `Parse(fixture) → Serialize → Parse → DeepEquals(original)`.
- **Unknown-field round-trip** test: take a fixture, inject an unknown key, parse + serialize + parse, assert unknown key preserved.
- **Malformed input** test: feed truncated JSON, assert parse fails cleanly (specific exception type, no partial model state).
- **Empty / minimal input** test: empty arrays, missing optional keys, null vs absent.
- **Encoding** test: same fixture as UTF-8 + UTF-8-BOM + CRLF + LF, all parse identically.

### Serializer added or modified

- **Round-trip** test (same as parser, opposite direction).
- **BOM-absence** test: serialize, read raw bytes, assert first 3 bytes are not `EF BB BF`.
- **Line-ending** test: assert configured EOL (CRLF for save-file outputs).
- **Indentation** test: assert tabs for save-file outputs (game-style).

### Save-file mutator added or modified

- **Backup-created** test: invoke mutator on a fixture (copied to a temp dir), assert `<File>.iuut-backup-<ts>` exists with the original bytes.
- **Re-parse-after-write** test: assert mutator re-reads and re-parses post-write.
- **Restore-on-failure** test: inject a re-parse failure (e.g., by mocking the parser), assert original file is restored from backup, assert backup is deleted (or retained per config — but the post-state is documented).
- **Timestamp uniqueness** test: invoke mutator twice within the same second, assert backup filenames don't collide (use injectable clock with sub-second resolution).

### Prospect blob codec (encode / decode)

- **Decode** test: fixture blob → uncompressed bytes → SHA-1 matches `ProspectBlob.Hash`.
- **Encode round-trip** test: decode → no-op mutation → encode → decode → bytewise equal.
- **Adler-32 trailer** test: encode an arbitrary uncompressed payload, parse the last 4 bytes as big-endian, assert == `Adler32(payload)`.
- **Header** test: assert first 2 bytes of encoded blob are `0x78 0x9C`.
- **Hash update** test: mutate uncompressed bytes, encode, assert `ProspectBlob.Hash` was updated to `SHA1(new_uncompressed_bytes)`.

### Validation rule added

- **Pass case** test: input that satisfies the rule, assert result.Ok == true.
- **Fail case** test for each failure mode: assert result.Ok == false, assert Issue list contains expected diagnostic.

### UI / WPF view added

- **Smoke test**: instantiate the view with a representative view-model in a headless WPF test host; assert no binding exceptions.
- **Manual screenshot** in PR description.

### Preset added (Lazy Max, Recovery, Custom sub-preset)

- **Integration test** against an anonymized fixture: invoke preset, assert expected files mutated, assert ValidationEngine pre/post checks ran, assert backups exist.
- **No-op case** test: invoke preset on a save that's already in the target state; assert no-op (no spurious backups, no spurious writes).

---

## §3. Fixture catalog

`fixtures/` (repo root, separate from `tests/`) holds anonymized save snippets. Subfolders:

```
fixtures/
├── canonical/         # Tagged snapshots used by snapshot tests; DO NOT EDIT without governance PR
├── profiles/          # Profile.json variants
├── characters/        # Characters.json variants
├── meta-inventory/    # MetaInventory.json variants
├── loadouts/          # Loadouts.json variants
├── associated/        # AssociatedProspects_Slot_*.json variants
├── prospects/         # Prospects/*.json variants (small + GUID-named)
├── prospect-blobs/    # Standalone decoded blob bytes for codec tests
├── mounts/            # Mounts.json variants
├── flags/             # flags_*.dat (binary, with hex dump comment)
├── accolades/         # Accolades.json variants
├── bestiary/          # BestiaryData.json variants
└── corrupted/         # Intentionally broken inputs for failure-mode tests
```

### Fixture provenance rules

- Every fixture has an adjacent `.md` note: `<fixture>.json.md` describing what it represents and what edge case it exercises.
- Every fixture's `.md` cites: shape source (live save? hand-crafted? derived from another fixture?), DataVersion, anonymization audit date.
- Fixtures in `canonical/` are referenced by snapshot tests; changes require a governance-tier PR per `SCOPE_GUARDRAILS.md` §2.9.

---

## §4. Fixture creation procedure

To derive a fixture from a live save:

1. **Copy** the source file to a working location outside the repo.
2. **Run** `pwsh -File scripts/anonymize-fixture.ps1 <source> <destination>` (to be authored as the first fixture is added — out of scope for governance v1 but committed when first needed).
3. **Manually review** the output against `SECURITY_PROTOCOL.md` §3 — automation cannot catch all PII (your own character name, etc.).
4. **Re-parse** the anonymized fixture with the production parser; assert round-trip equivalence.
5. **Commit** with an adjacent `.md` note.

---

## §5. Test data discipline

- **No `[InlineData]`** fixtures with SteamID64-shaped values (`7656119...`) — even for tests of the regex itself, use the documented placeholder.
- **No reading from `%LOCALAPPDATA%`** in any test. Tests use `fixtures/` or in-memory data.
- **No `Thread.Sleep`** in tests. Use injectable clock or `await Task.Delay` with cancellation.
- **No production-build code in test assemblies** beyond `InternalsVisibleTo`.

---

## §6. Coverage targets

- **`IUUT.Core` parsers / serializers / mutators:** ≥ 90% line coverage. Below 90% requires PR justification.
- **`IUUT.Core` services / presets:** ≥ 80% line coverage.
- **`IUUT.App`:** smoke-test only; coverage not measured.
- **CI** reports coverage but does not fail the build below a threshold (coverage is a tool, not a gate). The gate is the *contract* in §2 — specific tests for specific change types.

---

## §7. Test naming

`MethodName_Condition_ExpectedResult`:

- `ParseProfile_WithUnknownMetaRow_PreservesKey`
- `WriteCharacters_OnReParseFailure_RestoresFromBackup`
- `EncodeProspectBlob_AppendsBigEndianAdler32Trailer`
- `ValidateProfile_WhenUserIdMismatchesFolder_ReturnsFailingIssue`

Avoid: `Test1`, `ItWorks`, `RoundTripTest` (which round trip?).

---

## §8. CI invocation

`.github/workflows/governance-check.yml` covers governance; a sibling `.github/workflows/test.yml` (to be added when the first code lands) covers the test suite. CI failure on either workflow blocks merge.

Local invocation:

```powershell
dotnet test tests/IUUT.Core.Tests/IUUT.Core.Tests.csproj --logger "console;verbosity=minimal"
```

---

## §9. Manual checklist (UI / game-load)

`tests/MANUAL_CHECKLIST.md` lists the human-only steps required before a release:

- Launch IUUT.exe on a clean Windows 11 VM.
- Confirm save discovery, profile dropdown with PersonaName.
- Run Lazy Max on a copy of a real save.
- Launch Icarus, confirm the game loads, confirm character list intact, confirm clamp behavior matches `Icarus-Analysis.md` §10.

The PR reviewer ticks the relevant items for the change being reviewed.

---

## Revision history

| Version | Date | Change |
| --- | --- | --- |
| 1.0.0 | 2026-05-25 | Adopted. Test categories + mandatory tests per change type established. |
