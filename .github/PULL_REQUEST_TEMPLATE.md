<!--
GOVERNANCE NOTICE — Read before filling this out.

This template is enforced by .github/workflows/governance-check.yml.
Every section below is REQUIRED. Empty sections will block merge.

If you have not yet read AGENTS.md and the .agent/ folder, stop and read them
first. Filling out this template without reading the contract is itself a
governance violation.
-->

## Summary

<!-- 1–3 bullets. What does this PR change, in plain language? -->
- 
- 

## Spec authorization

<!--
Which spec section(s) authorize this change? Cite by doc + section number.
If no spec authorization exists, this PR likely belongs in `governance-amendment`
or `requires-human-approval` territory — see .agent/SCOPE_GUARDRAILS.md §2.

Examples:
- docs/IUUT-PROJECT-DOCUMENTATION.md §8.2 (Profile.json shape)
- docs/IUUT-PROJECT-DOCUMENTATION.md §13.1 (hard validation)
- Icarus-Analysis.md §10 (UTF-8 no BOM)
-->



## Consultation

<!--
List every governance and spec doc you read while preparing this change.
Minimum: AGENTS.md and .agent/CONSTITUTION.md (always required).
Add spec sections as relevant.

This mirrors the `Consulted:` commit trailer.
-->

- AGENTS.md
- .agent/CONSTITUTION.md
- 

## Agent

<!-- Identity of the agent that produced this PR. Examples:
     claude-code/2.1.149
     codex-cli/0.42
     cursor/0.45
     antigravity/0.x
     human-impanick
-->

Agent: 

## Files touched

<!-- Explicit list, no globs. -->



## Test plan

<!--
What tests run for this change? Manual verification steps if any.
For doc-only changes, "internal links verified, revision history updated" is acceptable.
For code changes, list test files / methods added or modified.
See .agent/TESTING_CONTRACT.md §2 for the per-change-type bar.
-->



## Definition-of-Done checklist

<!--
Tick each box that applies (see .agent/DEFINITION_OF_DONE.md).
DO NOT remove unticked boxes — leave them visible so reviewers can see what was N/A vs incomplete.
-->

Universal (every change):

- [ ] Branch name follows `HANDOFF_PROTOCOL.md` §1 (`agent/<agent>/<task-slug>`)
- [ ] Every commit has `Agent:`, `Consulted:`, `Co-Authored-By:` trailers
- [ ] `scripts/governance-lint.ps1` passes
- [ ] No `--no-verify` or unjustified `Governance-Override:` trailers
- [ ] No new external dependencies (or escalated per `SCOPE_GUARDRAILS.md` §2.6)
- [ ] Existing tests pass

For doc-only changes:

- [ ] Internal links resolve
- [ ] Revision history updated where applicable
- [ ] Anonymization holds (no Steam IDs, character names, personal paths)

For code changes:

- [ ] Style per `.agent/CODE_STYLE.md`
- [ ] Logging complies with `.agent/SECURITY_PROTOCOL.md` (no PII)
- [ ] Unknown JSON fields preserved on round-trip (CONSTITUTION VI)

For parser changes:

- [ ] Round-trip test against ≥1 anonymized fixture
- [ ] Unknown-field round-trip test
- [ ] Malformed-input test

For save-file mutators:

- [ ] Backup-created test
- [ ] Re-parse-after-write test
- [ ] Restore-on-failure test
- [ ] UTF-8 without BOM (no `[System.Text.Encoding]::UTF8`)

For prospect blob codec:

- [ ] `ZLibStream` used (not hand-stitched `DeflateStream`)
- [ ] Adler-32 trailer asserted big-endian
- [ ] `78 9C` header asserted
- [ ] SHA-1 recomputed on uncompressed bytes

## Drift declarations

<!--
Anything you did differently from a pattern an earlier agent established,
or from the spec. For each: name the deviation, cite the earlier pattern
(file:line or commit sha), and cite the CONSTITUTION article authorizing
the change.

If none, write "None."
-->

None.

## Hand-off notes

<!--
What the reviewer / next agent needs to know.
- What is done (verified state).
- What is next (immediate next step if anything).
- What must not be broken (any subtle invariant this change relies on).
-->



## Drift / Governance flags

<!-- Check any that apply -->

- [ ] `experimental` (per `.agent/AGENT_REGISTRY.md` scope)
- [ ] `requires-human-approval` (per `.agent/SCOPE_GUARDRAILS.md` §2)
- [ ] `governance-amendment` (per `.agent/AMENDMENT_PROCESS.md`)
- [ ] `cross-cutting` (blocks other agents from concurrent cross-cutting work)
- [ ] `emergency` (only with `Governance-Override:` per CONSTITUTION VIII.4 — must have retroactive review)

---

<!--
By submitting this PR, you (the agent or human author) assert that:
1. You have read AGENTS.md and the relevant .agent/ docs in full.
2. Every box above that applies to this change is ticked.
3. The PR honestly represents the work; `ready-for-review` is not used loosely.

Per CONSTITUTION VIII, this PR will be rejected if these assertions are false.
-->
