# Fixture: characters-basic.json

| | |
| --- | --- |
| **Shape source** | Synthetic, built to match the live `Characters.json` structure verified 2026-05-30. |
| **Container** | The nested-stringified wrapper: outer object with the single key `"Characters.json"` whose value is an array of JSON-**stringified** character objects (field guide §4). |
| **Contents** | 3 characters (`Char1`/`Char2`/`Char3`). Each has the **corrected** 13-field integer `Cosmetic` block, `TimeLastPlayed`, `XP_Debt`, a small `Talents` list, and `UnlockedFlags`. `Char3` carries an extra top-level member `ExperimentalCharField` to exercise unknown-member round-trip (CONSTITUTION VI). |
| **Anonymization** | Fully synthetic — no real Steam IDs, character names, prospect names, or paths (`LastProspectId`/`Location` blanked). SECURITY_PROTOCOL §3. |
| **Validation** | Parsed + round-tripped through the compiled `CharactersParser`/`CharactersSerializer` before commit (3 chars, cosmetic, unknown preserved). |
| **Format** | Generated JSON; LF in repo (parser tolerates CRLF/LF, TESTING_CONTRACT §2). |

Consumed by `CharactersRoundTripTests`. The unit-level cases build their own inline
containers (via a `Dictionary<string,string[]>`) so escaping is never hand-written.
