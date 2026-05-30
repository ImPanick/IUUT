# Fixture: bestiary-basic.json

| | |
| --- | --- |
| **Shape source** | Matches the live `BestiaryData.json` (verified 2026-05-30): 2 top-level keys. |
| **Contents** | `BestiaryTracking` (2 creature groups) + `FishTracking` (1 record with the verified `{FishRow, MaxQuality, MaxWeight, MaxLength, CaughtCount}` shape). |
| **Exercises** | `BestiaryTracking` parse + `NumPoints`; `FishTracking` element modeling; round-trip. |
| **Anonymization** | Fully synthetic — public-style row names, no PII. SECURITY_PROTOCOL §3. |
