# SCOPE_GUARDRAILS — What you may, may not, and must escalate

> **Tier 1 — Binding.**
> Read in full at the start of every session.

| | |
| --- | --- |
| **Document** | Scope Guardrails |
| **Version** | 1.0.0 |
| **Amendment rule** | `.agent/AMENDMENT_PROCESS.md` (normal PR + 1 reviewer) |

This document defines what is **in scope** for autonomous agent action, what is **out of scope** (human approval required), and the **escalation triggers** that pause work.

---

## §1. In scope (proceed autonomously per workflow)

You may, following `AGENT_WORKFLOW.md`, without separate human approval:

1. **Doc-only changes** that:
   - Correct typos, broken links, or formatting in existing docs.
   - Add empirical evidence to `Icarus-Analysis.md` from observed live saves (with anonymization per CONSTITUTION VII).
   - Update revision history tables when a change has been made.
   - Cross-reference an existing decision elsewhere in the doc set.
2. **Code changes** that implement a feature explicitly listed in a spec section that is past its design-locked status (see master doc §6.1 *Locked decisions* — those are fair game).
3. **Refactors within a single file or a single feature module** that preserve behavior and pass all existing tests, and whose PR body explains why the refactor improves clarity or maintainability.
4. **Test additions** that increase coverage of existing behavior without changing public API.
5. **Fixture additions** that are properly anonymized per CONSTITUTION VII and add coverage for an observed edge case.
6. **Build / tooling tweaks** that do not affect runtime behavior (e.g., `.editorconfig` adjustments, CI workflow improvements that are not the governance gate itself).

## §2. Out of scope — human approval required

You **must** open a PR labeled `requires-human-approval` (and not merge without explicit human sign-off) for any of:

1. **Changing a locked decision** in master doc §6.1 (Q1–Q9). Examples: switching from .NET 8 to .NET 9, dropping WPF for MAUI, removing the offline-first principle.
2. **Modifying `.agent/CONSTITUTION.md`** — full amendment process per `AMENDMENT_PROCESS.md`.
3. **Adding any network call** not enumerated in master doc §7.5.1 (Steam Web API resolver).
4. **Adding telemetry, analytics, crash reporting, auto-update phone-home, or any persistent identifier mechanism** — explicitly prohibited by CONSTITUTION V; an amendment PR is the only path.
5. **Adding authentication / account / login flows.**
6. **Introducing a new external dependency** (NuGet package, npm package, native DLL) — must be justified in the PR with a security review and license check.
7. **Changing the save-file write protocol** (backup → write → re-parse → restore) defined in CONSTITUTION III.
8. **Removing a published API method** in `IUUT.Core` once `IUUT.Core` has a v0.1 release.
9. **Editing fixtures** that have been promoted to "canonical" in `tests/IUUT.Core.Tests/fixtures/canonical/`.
10. **Adding a new AI agent** to `.agent/AGENT_REGISTRY.md` and granting it scope.
11. **Disabling, weakening, or bypassing** any layer of the enforcement stack (commit hook, CI gate, lint script, PR template).
12. **Editing `LICENSE`** or any third-party-license attribution.

## §3. Escalation triggers — stop and ask

These conditions require you to stop work immediately, mark the in-progress branch `wip-blocked`, and surface the issue (interactive: ask the user via your agent's question mechanism; non-interactive: open a `governance-question` issue):

1. **Spec contradiction.** Two spec sources say different things, or the spec contradicts a live observation.
2. **Spec gap.** A feature you must implement has no spec authorization.
3. **Save-file behavior surprise.** A parser succeeds in tests but produces unexpected output on a live save; or a writer's round-trip validation fails on a save you cannot reproduce.
4. **Catalog drift.** A `RowName` or `MetaRow` you encounter is not in the embedded catalog. (Per CONSTITUTION VI, you round-trip unknown fields, but the catalog drift itself should be surfaced.)
5. **DataVersion change.** `Profile.DataVersion` advances beyond what the spec acknowledges (currently 4 / Mendel). Halt, escalate, re-validate every behavior.
6. **Game-build update.** The game has shipped a new update since the spec was last verified. Document the build name and date; do not assume parity.
7. **Multi-agent conflict.** Your work conflicts with a pattern an earlier agent established and you cannot resolve it via `CODE_STYLE.md` lookup.
8. **Constitution ambiguity.** Two articles seem to be in tension. Surface; do not pick one.
9. **PII discovery.** You find real Steam IDs, character names, or paths in a committed file or fixture. Surface and clean up; do not commit fixes silently because the audit trail matters.
10. **Hook bypass detected.** You see a recent commit with `--no-verify` or with missing trailers. Surface; do not amend other agents' commits.

## §4. Escalation procedure

1. **Stop the current task.** Do not commit a guess.
2. **Determine session type:**
   - **Interactive (user present):** Use your agent's question mechanism. State the issue concisely:
     - What you were trying to do.
     - The contradiction / gap / surprise.
     - Two or more interpretations you considered.
     - What you recommend, if you have a preference.
   - **Non-interactive (autonomous):** Open a GitHub issue tagged `governance-question` with the same content. Mark your branch `wip-blocked`.
3. **Record the escalation** in your final commit's `Handoff-Notes:`.
4. **Wait for human resolution.** Do not poll. Do not retry. When the human resolves it, the path forward is either a spec amendment, a CONSTITUTION amendment, or a `GOVERNANCE_DECISIONS.md` entry that codifies the answer.

## §5. The "obvious fix" trap

> If you find yourself reasoning "this is obviously the right thing to do, I'll just do it," **stop**. That reasoning is the most common path to multi-agent drift.

The Constitution and these guardrails exist precisely because "obvious" decisions made independently by different agents produce contradictory results. The fix takes 30 seconds and an `AskUserQuestion` call. The drift takes weeks to unwind.

If you genuinely believe a change is so obviously correct that escalation is wasteful, write a one-paragraph case in your PR body's "Drift declarations" section and proceed. The PR review will judge whether your judgment was correct, and the answer will improve calibration for next time.

---

## Revision history

| Version | Date | Change |
| --- | --- | --- |
| 1.0.0 | 2026-05-25 | Adopted alongside CONSTITUTION v1.0.0. |
