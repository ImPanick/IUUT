# CONSTITUTION — IUUT Multi-Agent Governance

> **Tier 0 — Immutable.**
> Articles I–X are normative. They may be amended only through the procedure in `AMENDMENT_PROCESS.md` (labeled PR + explicit human approval + revision-history entry). Silent edits to this file are governance violations and must be reverted.

| | |
| --- | --- |
| **Document** | IUUT Constitution |
| **Version** | 1.0.0 |
| **Adopted** | 2026-05-25 |
| **Amendment rule** | `.agent/AMENDMENT_PROCESS.md` |

---

## §0. Preamble

This codebase will be touched by multiple AI agents — currently Claude Code (primary), OpenAI Codex, Cursor, and Google Antigravity, and likely others in the future. Without a shared, enforceable contract, these agents will:

- Introduce contradictory abstractions, naming, and file layouts.
- Bypass the documentation-first discipline that makes IUUT safe to ship.
- Silently degrade the save-format spec by inventing behaviors not present in the live data.
- Smuggle telemetry, cloud upload, or auth into a project that has deliberately rejected those things.
- Hand off mid-task in undocumented states that other agents cannot recover from.

This Constitution exists to make those failure modes **mechanically expensive**, not merely impolite. It is enforced by commit hooks, CI gates, and review gates — not by trust.

---

## Article I — Documentation-first

> The specification is the deliverable until a specification section explicitly opens an implementation phase.

1. The authoritative specification is, in order of precedence:
   1. `docs/IUUT-PROJECT-DOCUMENTATION.md` (master spec)
   2. `Icarus-Analysis.md` (save-format field guide)
   3. `docs/icarus-save-editor-gameplan.md` (legacy gameplan — historical context only; do not derive new behavior from this)
   4. `docs/max-icarus-characters_47df3b52.plan.md` (PowerShell POC plan — validated, but scope-limited to the proven mutation)
2. Code that implements a feature **must** cite the spec section that authorizes the feature. PRs without such a citation are rejected.
3. Code that diverges from the spec is a governance violation **unless** the same PR also updates the spec to match and the update is approved per `AMENDMENT_PROCESS.md`.

## Article II — Empirical truth

> Claims about save format, game behavior, or runtime semantics must be backed by `Icarus-Analysis.md` or by a live observation reproducible in the same PR.

1. "I think the game does X" is not evidence. "Live save file at `<path>` contains X; reproduce with `<command>`" is evidence.
2. If reality contradicts the spec, the agent escalates per `SCOPE_GUARDRAILS.md` §3. The agent does **not** silently rewrite the spec to match what it observed; the contradiction must be visible.
3. Inferred behaviors must be labeled `(inferred)` in code comments and spec text, per the field-guide convention.

## Article III — Safety-first save-file writes

> Every save-file mutator must follow the protocol: **backup → write → re-parse → restore-on-failure.** No exceptions.

1. Before any write to a file under `%LOCALAPPDATA%\Icarus\Saved\PlayerData\`, the mutator creates `<File>.iuut-backup-<YYYYMMDD-HHMMSS>` alongside the original.
2. After the write, the mutator re-reads the file and re-parses it via the same parser used to load it.
3. If the re-parse fails, the mutator restores from the backup and surfaces the error. It does **not** leave a partially-written file in place.
4. This protocol applies to **all** save-file writes, including those that "obviously" succeed. The check is cheap; corrupting a player's save is not.

## Article IV — Honest scope

> No fabricated features, no speculative APIs, no behavior the spec did not describe.

1. If a feature is not in the spec, the agent does not implement it. The agent proposes a spec change first.
2. If a public API method is needed but not in the spec, the agent surfaces the gap; the agent does not invent the signature.
3. Wireframes, mockups, and examples in the spec are illustrative unless explicitly marked normative. Treat them as a guide to intent, not as a frozen interface.

## Article V — Offline-first, no telemetry

> IUUT is a local-only tool. The following are categorically prohibited and may not be added by any agent under any circumstances absent a CONSTITUTION amendment:

1. **Telemetry, analytics, crash reporting** of any form.
2. **Cloud upload** of save files or any derived data.
3. **Account authentication** flows (the optional Steam Web API call for PersonaName resolution is the **only** outbound network call permitted; it is enumerated in master doc §7.5.1 and uses a user-provided API key).
4. **Persistent identifiers** (device fingerprints, install IDs, hashed user IDs) beyond what the user-provided Steam API key implicitly carries.
5. **Auto-update mechanisms** that phone home with version data without explicit user opt-in.

If an agent believes one of these should be added, the path is an amendment PR with a full threat-model justification — not a commit.

## Article VI — Forward compatibility

> Unknown fields in save files round-trip verbatim. The game's schema may evolve; we do not lose user data because the spec lags.

1. JSON parsers use `System.Text.Json` extension data (or equivalent) so unknown keys are preserved on serialization.
2. Unknown `RowName`, `MetaRow`, and `PropertyType` values are written back unchanged even when not present in the embedded catalog.
3. `Profile.DataVersion` is read-only to the editor; we preserve whatever the game wrote.

## Article VII — Anonymization

> Real Steam IDs, character names, PersonaNames, and personally-identifiable paths must never appear in committed files.

1. Fixtures are scrubbed: SteamID64 → `00000000000000000`; character names → generic (`Char1`, `Char2`); PersonaName → `TestUser`.
2. Commit messages, PR bodies, screenshots, and code comments may not contain real Steam IDs or names.
3. Logs in shipped builds may not contain real Steam IDs (PersonaName in user-facing UI is fine; in log files is not).
4. `scripts/governance-lint.ps1` enforces this via regex (SteamID64 pattern: `7656119\d{10}`).

## Article VIII — Consultation-mandatory

> Every change cites the doc sections that were read and authorized the change.

1. Every commit message ends with `Consulted: <doc>#<section>[, <doc>#<section>...]`.
2. Every PR body fills the consultation section of `.github/PULL_REQUEST_TEMPLATE.md`.
3. The minimum consultation for any change is `Consulted: AGENTS.md, .agent/CONSTITUTION.md` plus whatever spec section authorized the change.
4. Bypassing the consultation requirement requires a `Governance-Override: <CONSTITUTION article reference + justification>` trailer in the commit. Overrides are reviewed.

## Article IX — Conservative defaults

> Warn, do not hard-block. Refuse to corrupt, do not refuse to assist.

1. UI: when in doubt, warn the user and let them proceed. The exception is hard-fail validation per master doc §13.1 (round-trip parse failure, UserID mismatch, etc.) — those block writes.
2. Soft warnings (master doc §13.2): show, confirm, proceed. The user owns their save.
3. Game-state detection (master doc §14): warn, never block. The game running is not a reason to refuse all edits.
4. Apply this principle to agent behavior too: when in doubt about scope, ask the user, do not refuse the task.

## Article X — Multi-agent coordination

> Every agent declares its identity, cites its sources, and hands off explicitly.

1. **Identity:** every commit has `Agent: <name>/<version>` trailer. Examples: `Agent: claude-code/2.1.149`, `Agent: codex-cli/0.42`, `Agent: cursor/0.x`, `Agent: antigravity/0.x`.
2. **Citation:** every commit has `Consulted:` trailer (Article VIII).
3. **Branch naming:** `agent/<short-agent>/<short-task>` — e.g., `agent/claude/profile-parser`, `agent/codex/lazy-max-service`.
4. **Hand-off:** when an agent stops mid-task, the final commit's body ends with `Handoff-State:` and `Handoff-Notes:` per `HANDOFF_PROTOCOL.md` §6.
5. **Drift declarations:** when an agent deviates from a pattern an earlier agent established, the PR body's "Drift declarations" section names the deviation, the earlier pattern, and the CONSTITUTION article authorizing the change.
6. **No silent third pattern:** if two agents have established conflicting patterns, the resolution is a `CODE_STYLE.md` amendment, not a third pattern introduced by a third agent.

---

## Revision history

| Version | Date | Change |
| --- | --- | --- |
| 1.0.0 | 2026-05-25 | Adopted. Articles I–X established. |

---

*Amendment procedure: `.agent/AMENDMENT_PROCESS.md`. Reading-order index: `.agent/README.md`.*
