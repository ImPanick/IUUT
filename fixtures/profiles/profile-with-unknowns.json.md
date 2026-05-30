# Fixture: profile-with-unknowns.json

| | |
| --- | --- |
| **Shape source** | Hand-crafted to exercise forward compatibility (CONSTITUTION VI). |
| **DataVersion** | 5 (a deliberately *future* value) |
| **Exercises** | Unknown-field preservation: an unknown `MetaRow` (`Exotic_Yellow`), an unknown `Talents` RowName (`Workshop_Future_Widget`), a future `DataVersion` (5), and an **unknown top-level object** (`FutureAccountField`). A correct parser must round-trip all of these **verbatim** via extension data. |
| **Anonymization audit** | 2026-05-25. `UserID` → sentinel. No real data — entirely synthetic. |
| **Format** | Tab-indented, LF in repo. |

The unknown-field round-trip test (DoD §4, TESTING_CONTRACT §2) asserts that
serializing the parsed model reproduces `Exotic_Yellow`, `Workshop_Future_Widget`,
`DataVersion: 5`, and the entire `FutureAccountField` object unchanged.
