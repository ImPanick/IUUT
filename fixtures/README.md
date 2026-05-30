# fixtures/

Anonymized save-file snippets and binary blobs for the test suite.

**Authority:** `.agent/TESTING_CONTRACT.md` §3, §4; `.agent/SECURITY_PROTOCOL.md` §3, CONSTITUTION VII.

## Hard rules

1. **Zero PII.** No real SteamID64. No real character names. No real PersonaName. No personal file paths. See `SECURITY_PROTOCOL.md` §3 for the exact scrub mapping.
2. **Anonymization audit before commit.** Run `scripts/governance-lint.ps1` over the diff. Manual review for character names (lint cannot know yours).
3. **Provenance note required.** Every fixture has an adjacent `<fixture>.md` describing shape source, DataVersion, edge case exercised, anonymization audit date.
4. **Fixtures in `canonical/` are governance-tier.** Changes require a PR labeled `requires-human-approval` per `SCOPE_GUARDRAILS.md` §2.9.

## Layout (per TESTING_CONTRACT §3)

| Folder | Purpose |
| --- | --- |
| `canonical/` | Tagged snapshots used by snapshot tests. **Edit only via governance PR.** |
| `profiles/` | `Profile.json` variants — every known `MetaRow`, `UnlockedFlags` shapes, talent counts. |
| `characters/` | `Characters.json` variants — single character, three characters, mid-prospect character, dead/abandoned. |
| `meta-inventory/` | `MetaInventory.json` variants — empty, full, GUID-coupled with `loadouts/`. |
| `loadouts/` | `Loadouts.json` variants — empty, single drop, multi-prospect, with `.<N>.backup` naming. |
| `associated/` | `AssociatedProspects_Slot_N.json` variants — active, completed, stuck-character. |
| `prospects/` | `Prospects/<name>.json` — fresh outpost (small), heavily-built (large), GUID-named. |
| `prospect-blobs/` | Standalone decoded blob bytes for codec round-trip tests. |
| `mounts/` | `Mounts.json` variants. |
| `flags/` | Binary `flags_*.dat` fixtures (anonymized SteamID at offset 4). |
| `accolades/` | `Accolades.json` variants — empty, partial, all completed. |
| `bestiary/` | `BestiaryData.json` variants. |
| `corrupted/` | Intentionally broken inputs for failure-mode tests. |

## Fixture creation procedure

To derive a fixture from a live save (when `scripts/anonymize-fixture.ps1` is implemented):

1. Copy source file to a working location **outside** the repo.
2. Run `pwsh -File scripts/anonymize-fixture.ps1 <source> <destination>`.
3. **Manually** review output against `SECURITY_PROTOCOL.md` §3 — automation can't catch all PII (your own character names, etc.).
4. Re-parse with the production parser; assert round-trip equivalence.
5. Commit with an adjacent `.md` provenance note.

This is a scaffold; no fixtures have been added yet.
