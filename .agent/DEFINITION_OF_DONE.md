# DEFINITION_OF_DONE — The acceptance bar

> **Tier 1 — Binding.**
> A change is "done" only when every applicable item below is true. A PR that claims `ready-for-review` without meeting this bar is a governance violation.

| | |
| --- | --- |
| **Document** | Definition of Done |
| **Version** | 1.0.0 |
| **Amendment rule** | `.agent/AMENDMENT_PROCESS.md` |

---

## §1. Universal acceptance criteria (every change)

- [ ] **Branch name follows `HANDOFF_PROTOCOL.md` §1.**
- [ ] **Every commit has** `Agent:`, `Consulted:`, `Co-Authored-By:` trailers (enforced by `commit-msg` hook).
- [ ] **PR body uses `.github/PULL_REQUEST_TEMPLATE.md`** with every section filled.
- [ ] **PR labels** include `agent:<agent>` and one state label (`ready-for-review`, `wip-blocked`, `experimental`, `governance-amendment`, or `requires-human-approval`).
- [ ] **`scripts/governance-lint.ps1` passes** on the diff (no PII; no hardcoded paths; no missing trailers).
- [ ] **No `--no-verify` or `Governance-Override:`** unless explicitly justified in the PR body and accepted by the reviewer.
- [ ] **No new external dependencies** without `SCOPE_GUARDRAILS.md` §2.6 escalation.
- [ ] **Existing tests still pass.** If the change breaks tests, either fix the tests (with a justification) or fix the change.

---

## §2. Doc-only changes

In addition to §1:

- [ ] **Internal links resolve.** Markdown links to other docs in this repo point at real files / sections.
- [ ] **Cross-references are reciprocal** where appropriate (if doc A now cites doc B, doc B's revision history should note the cross-reference if it materially relies on it).
- [ ] **Revision history table is updated** in any doc that has one and was substantively changed.
- [ ] **Anonymization holds** — no Steam IDs / character names / personal paths in new content (`scripts/governance-lint.ps1` runs the regex).
- [ ] **Citations** in prose are concrete: doc + section, not vague "see the spec."

---

## §3. Code changes (general)

In addition to §1:

- [ ] **Code style** matches `.agent/CODE_STYLE.md`. (Lint / formatter passes.)
- [ ] **Public types and methods have XML doc comments.** Internal types need them only when non-obvious.
- [ ] **No dead code.** Unused `using`s, unreachable branches, commented-out blocks removed.
- [ ] **Logging** complies with `.agent/SECURITY_PROTOCOL.md` (no PII, no Steam IDs).
- [ ] **Error handling** follows the safety-first principle (CONSTITUTION III): no silent catches; failures propagate with context.
- [ ] **Unknown-field preservation** verified for any JSON parser change (CONSTITUTION VI).
- [ ] **The spec section that authorized this change** is cited in the PR's "Spec authorization" field.

---

## §4. Parser changes (specific)

Additional requirements for any change to a `*Parser` or `*Serializer`:

- [ ] **Round-trip test** against ≥1 anonymized fixture in `tests/IUUT.Core.Tests/fixtures/`.
- [ ] **Unknown-field round-trip test** — parse a fixture with an added unknown key, serialize, assert the unknown key is preserved verbatim.
- [ ] **Malformed-input test** — parse a known-bad fixture, assert it fails cleanly (no partial state, no swallowed exception).
- [ ] **Encoding verified** — parser handles UTF-8 with and without BOM, CRLF and LF line endings, tab and space indentation (per `Icarus-Analysis.md` §1).

---

## §5. Save-file mutator changes (specific)

Additional requirements for any code that **writes** to a save file:

- [ ] **Backup created** before write — verified by test that asserts `<File>.iuut-backup-<timestamp>` exists post-call.
- [ ] **Backup naming format** matches `<File>.iuut-backup-<YYYYMMDD-HHMMSS>` per master doc §7.6 and `Icarus-Analysis.md` §10.
- [ ] **Re-parse after write** — test confirms the mutator re-reads and re-parses the file before returning success.
- [ ] **Failure restores from backup** — test that injects a re-parse failure and asserts the original file is restored from backup.
- [ ] **UTF-8 without BOM** — uses `(New-Object System.Text.UTF8Encoding $false)` in PowerShell or `new UTF8Encoding(false)` in C#. Never `[System.Text.Encoding]::UTF8` or `Encoding.UTF8` (those emit a BOM).
- [ ] **Round-trip-validated** before the mutator is even invoked — ValidationEngine pre-check per master doc §13.1 ran and passed.

---

## §6. Prospect blob changes (specific)

Additional requirements for any code touching `ProspectBlob.BinaryBlob` (encode or decode):

- [ ] **`ZLibStream` used** for the full wrapper, not hand-stitched `DeflateStream` + manual header bytes (per `Icarus-Analysis.md` §8.1 *Recompression*).
- [ ] **Adler-32 trailer** present on every re-encoded blob — test asserts last 4 bytes match `Adler32(uncompressed_bytes)` in big-endian.
- [ ] **`78 9C` header** present on every re-encoded blob.
- [ ] **SHA-1 hash recomputed** on uncompressed bytes after any mutation; `ProspectBlob.Hash` updated; `ProspectBlob.UncompressedLength`, `TotalLength`, `DataLength` updated.
- [ ] **Round-trip on a real-shape fixture** (decode → no-op → re-encode → re-decode → bytewise equal).

---

## §7. UI / WPF changes

Additional requirements for any view, view-model, or control change:

- [ ] **Compiles without binding errors** — `<binding-warnings-as-errors>` enabled in the WPF project.
- [ ] **Smoke test** runs the view in a headless test harness; asserts no runtime binding exceptions on a representative view-model.
- [ ] **Manual screenshot** in the PR description for human review (governance: visual changes need eyes).
- [ ] **DPI behavior verified** at 100% and 150% if layout was touched.
- [ ] **No PII in design-time data** — design-time view-models use `TestUser`, `Char1`, scrubbed SteamIDs.

---

## §8. Test changes

Additional requirements for changes to `tests/`:

- [ ] **New tests are deterministic** — no real-time dependencies, no network calls, no `DateTime.Now` without injection.
- [ ] **Fixtures are anonymized** per CONSTITUTION VII.
- [ ] **Test names** follow `MethodName_Condition_ExpectedResult` (e.g., `ParseProfile_WithUnknownMetaRow_PreservesKey`).
- [ ] **No commented-out tests.** Either fix them or delete them with a citation.

---

## §9. Fixture changes

Additional requirements for changes to `tests/IUUT.Core.Tests/fixtures/`:

- [ ] **Anonymization audit** — every Steam ID, character name, PersonaName replaced per `SECURITY_PROTOCOL.md` §3.
- [ ] **Fixture provenance note** in `fixtures/README.md` — what shape of save it represents, what edge case it exercises.
- [ ] **No binary blobs** unless cited as necessary for the test (a prospect blob fixture, e.g.); justify in PR.
- [ ] **License check** — if the fixture derives from a tool / community resource, the source is cited and the license is compatible with the project's MIT.

---

## §10. Governance changes (`.agent/` edits)

Additional requirements for any change to `.agent/`:

- [ ] **`AMENDMENT_PROCESS.md` followed** end-to-end.
- [ ] **PR labeled `governance-amendment`** and `requires-human-approval`.
- [ ] **Two-signoff** for `CONSTITUTION.md` edits (one human + one agent of a different model from the proposer, or two humans).
- [ ] **Revision history table** in the amended doc gets a new row.
- [ ] **No other changes in the same PR** — governance amendments are mono-purpose.

---

## §11. CI / enforcement changes

Additional requirements for changes to `.githooks/`, `.github/workflows/`, `scripts/governance-lint.ps1`:

- [ ] **Self-test** — the new hook / lint / workflow is exercised against a sample commit before merge.
- [ ] **Does not weaken** any existing check (per `SCOPE_GUARDRAILS.md` §2.11).
- [ ] **Doc updated** — `.agent/README.md` enforcement table reflects the new state.

---

## §12. The "ready-for-review" claim

When you set `Handoff-State: ready-for-review`, you are asserting that **every applicable box above is checked**. This is auditable — reviewers will pick a sample and verify. False claims of `ready-for-review` damage the trust gradient between agents and humans; repeat false claims will result in the agent's scope being narrowed in `.agent/AGENT_REGISTRY.md`.

If you are not sure whether your work meets the bar, set `Handoff-State: wip-blocked` and ask. That is always a respectable choice.

---

## Revision history

| Version | Date | Change |
| --- | --- | --- |
| 1.0.0 | 2026-05-25 | Adopted. Acceptance bar codified per change type. |
