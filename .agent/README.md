# `.agent/` — IUUT Governance Folder

This folder contains the **binding contract** for any AI coding agent (Claude Code, OpenAI Codex, Cursor, Google Antigravity, or future agents) operating on this repository.

If you are an agent: you got here via `AGENTS.md` (root). Read this folder in the order below before any code action.

If you are a human: this is the operating system for multi-agent work on IUUT. The high-level reasoning is in `CONSTITUTION.md`; the operational rituals are in `AGENT_WORKFLOW.md`; the bureaucracy is in `AMENDMENT_PROCESS.md`.

---

## Reading order (canonical)

| # | File | Tier | Read when |
| --- | --- | --- | --- |
| 1 | `CONSTITUTION.md` | Tier 0 (immutable) | Every session, in full |
| 2 | `SCOPE_GUARDRAILS.md` | Tier 1 (binding) | Every session, in full |
| 3 | `AGENT_WORKFLOW.md` | Tier 1 (binding) | Every session, in full |
| 4 | `HANDOFF_PROTOCOL.md` | Tier 1 (binding) | Every session, in full |
| 5 | `DEFINITION_OF_DONE.md` | Tier 1 (binding) | Every session, in full |
| 6 | `CODE_STYLE.md` | Tier 2 (binding for code tasks) | Code tasks |
| 7 | `SECURITY_PROTOCOL.md` | Tier 2 | Any task touching save files, fixtures, logs, or commits |
| 8 | `TESTING_CONTRACT.md` | Tier 2 | Any code task |
| 9 | `AMENDMENT_PROCESS.md` | Tier 3 (meta) | Only when proposing a governance change |
| 10 | `AGENT_REGISTRY.md` | Tier 3 (meta) | Reference; consult when adding a new agent or determining another agent's scope |

---

## Tiers

- **Tier 0 — Immutable.** The CONSTITUTION. Amendment requires an explicit, labeled, human-approved PR per `AMENDMENT_PROCESS.md`. Articles I–X may not be silently edited.
- **Tier 1 — Binding.** The operational core. Amendment requires a normal PR with citation + 1 reviewer.
- **Tier 2 — Binding for relevant tasks.** Technical contracts. Amendment requires PR + reviewer; deviations within a single change must be justified in the PR body.
- **Tier 3 — Meta.** Process and registry. Amended like Tier 1.

---

## Enforcement

| Layer | Mechanism | Where |
| --- | --- | --- |
| Commit-time | `commit-msg` hook validates trailers | `.githooks/commit-msg` |
| Pre-PR | Governance lint scans for PII + style + completeness | `scripts/governance-lint.ps1` |
| CI (governance) | Re-runs lint + validates PR body + commit trailers | `.github/workflows/governance-check.yml` |
| CI (build) | Restore → build (warnings-as-errors) → test → format | `.github/workflows/build.yml` |
| Review routing | Owner auto-requested on governance + locked-decision paths | `.github/CODEOWNERS` |
| Review | Human required for CONSTITUTION + locked-decision changes | `AMENDMENT_PROCESS.md` |

**Operational runbooks** (how to actually build/test/release — reference, not contract):
`docs/DEVELOPMENT.md` (local dev) and `docs/CICD.md` (pipeline + branch protection + releases).

To wire the local hook after cloning:

```powershell
pwsh -File scripts/install-hooks.ps1
```

If you don't install it, your commits will pass locally but be rejected at PR review.

---

## Why this exists (short)

Multiple agents with different training and defaults will produce a contradictory codebase unless bound by a shared, enforceable, machine-readable contract. This folder is that contract. The cost of consultation is small. The cost of un-doing un-coordinated drift is large.

## Why this exists (longer)

See `CONSTITUTION.md` §0 — *Preamble*.

---

*This file is governed by `AMENDMENT_PROCESS.md`. Last updated: 2026-05-25.*
