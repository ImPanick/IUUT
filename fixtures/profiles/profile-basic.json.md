# Fixture: profile-basic.json

| | |
| --- | --- |
| **Shape source** | Anonymized from a live `Profile.json` (Mendel-era save). |
| **DataVersion** | 4 |
| **Exercises** | Happy-path parse/serialize round-trip: all 7 known `MetaRow` keys (incl. `Exotic_Uranium`), a 12-entry `UnlockedFlags`, two `Talents` (`Workshop_*` + `Prospect_*`), `NextChrSlot`, `DataVersion`. |
| **Anonymization audit** | 2026-05-25. `UserID` → sentinel `00000000000000000`. No real Steam IDs, names, or paths. Currency counts are non-identifying illustrative values. |
| **Format** | Tab-indented, LF in repo (game writes CRLF; parser must tolerate both — TESTING_CONTRACT §2). |

Used by `IUUT.Core.Tests` once the `Profile.json` parser lands (WP-2).
