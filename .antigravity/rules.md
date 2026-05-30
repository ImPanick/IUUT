# Google Antigravity Rules — IUUT Governance

> **Path note:** This file is placed at `.antigravity/rules.md` as a best-effort default for Google Antigravity's rules discovery. If the active Antigravity version expects a different path (e.g. `.antigravity/agents.yml`, `antigravity.config.md`, or a workspace-level setting), copy this file to the correct location and update this note in a governance-amendment PR.

You are operating on a **governed multi-agent repository**. Before any code action, you must consult the binding contract.

## Required reading

1. `AGENTS.md` (repo root) — the universal contract.
2. `.agent/CONSTITUTION.md` — articles I–X, immutable.
3. `.agent/SCOPE_GUARDRAILS.md` — what you can / cannot do.
4. `.agent/AGENT_WORKFLOW.md` — the ritual you must follow.
5. `.agent/HANDOFF_PROTOCOL.md` — commit/PR conventions; multi-agent coordination.
6. `.agent/DEFINITION_OF_DONE.md` — acceptance bar.
7. `.agent/CODE_STYLE.md`, `.agent/SECURITY_PROTOCOL.md`, `.agent/TESTING_CONTRACT.md` for code tasks.
8. `docs/IUUT-PROJECT-DOCUMENTATION.md`, `Icarus-Analysis.md` — the spec you implement against.

## Antigravity-specific scope restriction

Per `.agent/AGENT_REGISTRY.md`, Antigravity is currently classified **experimental** on this repo. Until that classification is upgraded via amendment:

- You **may**: read any file, propose docs-only changes, propose refactors to non-save-file code.
- You **must not without explicit human sign-off**: introduce or modify any save-file writer, any cryptographic / hash code (SHA-1 verification, Adler-32 trailer construction), any zlib codec code, any Steam API integration, or anything under `IUUT.Core/ProspectBlob/` once it exists.
- Any PR you open must be labeled `agent:antigravity` and `experimental` and explicitly tagged in the PR body's "Drift declarations" section.

This restriction exists because we have less operational experience with Antigravity in this codebase. It will be relaxed as track record accumulates. Argue the case for relaxation via `.agent/AMENDMENT_PROCESS.md`, not by ignoring it.

## Hard rules (full text in `.agent/CONSTITUTION.md`)

**Must:**
- `Agent: antigravity/<version>` and `Consulted: <doc>#<section>` trailers on every commit.
- Round-trip parser changes against a fixture before commit.
- Backup → write → re-parse → restore-on-failure for any save-file mutator.
- Anonymize fixtures.
- Preserve unknown JSON fields on round-trip.

**Must not:**
- Telemetry, cloud upload, auth, unenumerated network calls.
- Hardcoded usernames / paths / Steam IDs / character names.
- Bypass the `commit-msg` hook without a `Governance-Override:` trailer.
- Amend `.agent/CONSTITUTION.md` outside the amendment process.

## When stuck

Stop. Escalate per `.agent/SCOPE_GUARDRAILS.md` §3. Do not guess.
