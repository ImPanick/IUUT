# Fixture: accolades-basic.json

| | |
| --- | --- |
| **Shape source** | Matches the live `Accolades.json` (verified 2026-05-30): 3 top-level keys. |
| **Contents** | `CompletedAccolades` (2 entries) + `PlayerTrackers` + `PlayerTaskListTrackers` (small synthetic counter objects). |
| **Exercises** | `CompletedAccolades` parse; preservation of the two non-edited tracker objects through extension data (CONSTITUTION VI). |
| **Anonymization** | Fully synthetic — public-style accolade row names, sentinel prospect GUID, no PII. SECURITY_PROTOCOL §3. |
