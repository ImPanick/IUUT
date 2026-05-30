# AGENTS.md — Universal Agent Entry Point

> **You are reading this because you are an AI coding agent operating on this repository.**
> Read this file in full **before** any tool use, file edit, or commit.
> Then read every file in `.agent/` in the order listed below.
> Failure to do so will produce work that is rejected by the governance gate.

This document is the **non-negotiable contract** for any AI agent (Claude Code, OpenAI Codex, Cursor, Google Antigravity, or any future agent) that touches this repository.

---

## 1. What this repository is

**Icarus Ultimate Utility Tool (IUUT)** — an unofficial Windows desktop application for viewing, repairing, and editing local *Icarus* (RocketWerkz) save files. The project is in a **documentation-first** phase: the specification is the deliverable until §16 of the master doc explicitly opens an implementation phase.

The authoritative spec is:

| Doc | Role |
| --- | --- |
| `docs/IUUT-PROJECT-DOCUMENTATION.md` | Master spec (most authoritative) |
| `Icarus-Analysis.md` | Save-format field guide (technical source of truth) |
| `docs/icarus-save-editor-gameplan.md` | Legacy gameplan (superseded — historical context only) |
| `docs/max-icarus-characters_47df3b52.plan.md` | PowerShell POC plan (validated proof-of-concept) |

You do **not** invent features, APIs, file formats, or behaviors not present in those documents. If reality contradicts the documents, you escalate (see `.agent/SCOPE_GUARDRAILS.md`), you do **not** invent.

---

## 2. Mandatory reading order

Read these in order. Do not skip. Do not skim.

| # | File | What it gives you |
| --- | --- | --- |
| 1 | `.agent/README.md` | Map of the governance folder |
| 2 | `.agent/CONSTITUTION.md` | **The immutable principles.** Articles I–X. |
| 3 | `.agent/SCOPE_GUARDRAILS.md` | What you are / are not allowed to do |
| 4 | `.agent/AGENT_WORKFLOW.md` | The exact ritual: pre-flight → plan → implement → test → hand-off |
| 5 | `.agent/HANDOFF_PROTOCOL.md` | Branch / commit / PR conventions for multi-agent work |
| 6 | `.agent/DEFINITION_OF_DONE.md` | The acceptance bar |
| 7 | `.agent/CODE_STYLE.md` | When you write code |
| 8 | `.agent/SECURITY_PROTOCOL.md` | Handling Steam IDs, character names, secrets |
| 9 | `.agent/TESTING_CONTRACT.md` | What must be tested before merge |
| 10 | `.agent/AMENDMENT_PROCESS.md` | How (and when) governance itself may change |
| 11 | `.agent/AGENT_REGISTRY.md` | Who else has touched / may touch this repo |
| 12 | `docs/IUUT-PROJECT-DOCUMENTATION.md` | Master spec — read sections relevant to your task |
| 13 | `Icarus-Analysis.md` | Field guide — read sections relevant to your task |
| 14 | `docs/DEVELOPMENT.md` | **Code tasks:** how to build, test, run, and follow the change lifecycle locally |
| 15 | `docs/CICD.md` | **Code tasks:** the CI/CD pipeline, branch protection, and release process your work flows through |

For doc-only tasks you may stop at row 13. For code tasks you read everything (rows 14–15 are the operational runbooks).

---

## 3. The hard rules — short version

(Full normative text lives in `.agent/CONSTITUTION.md`. This is a summary, not a substitute.)

**You MUST:**

1. **Cite consulted sections** in every commit message via a `Consulted:` trailer (see `.agent/HANDOFF_PROTOCOL.md` §4). Commits without it are rejected by the `commit-msg` hook.
2. **Cite consulted sections** in every PR body via the template (`.github/PULL_REQUEST_TEMPLATE.md`). PRs without it are rejected by CI.
3. **Round-trip every parser change** against a fixture before commit.
4. **Backup before write** for every save-file mutator. No exceptions.
5. **Validate after write** (re-parse) and **restore on failure.** No silent failures.
6. **Anonymize all fixtures.** No real Steam IDs, character names, or PersonaNames in committed files.
7. **Preserve unknown fields** verbatim on round-trip (forward compatibility).
8. **Update the spec** when you change behavior the spec described. The spec and the code drift apart only across an amendment.
9. **Escalate** when reality contradicts the spec. Do not silently "fix" reality to match.
10. **Declare your agent identity** in commit and PR metadata.

**You MUST NOT:**

1. Add telemetry, analytics, crash reporting, cloud upload, account auth, or any network call not explicitly enumerated in the spec.
2. Hardcode usernames, paths, Steam IDs, or character names anywhere in the codebase.
3. Modify save files in ways that fail round-trip validation.
4. Bypass the `commit-msg` hook (`--no-verify`) without a `Governance-Override:` trailer that names a CONSTITUTION article allowing the override.
5. Amend `.agent/CONSTITUTION.md` without following `.agent/AMENDMENT_PROCESS.md`. Articles I–X are immutable absent an explicit governance amendment PR.
6. Speak with authority about save format details not present in `Icarus-Analysis.md` or verified against a live save in the same PR.
7. Mark a task complete when tests fail, the build is broken, or hooks are bypassed.

---

## 4. Multi-agent coordination

Multiple agents (Claude Code, Codex, Cursor, Antigravity) will operate on this repo. To avoid drift:

- **Every commit declares its agent.** `Agent: <name>/<version>` trailer.
- **Every commit declares its consultation.** `Consulted: <doc>#<section>[, <doc>#<section>...]`.
- **Branches are named** `agent/<agent>/<short-task>` so concurrent work is visible.
- **Hand-offs are explicit.** When you stop mid-task, the last commit must end with `Handoff-State: <ready-for-review|wip-blocked|awaiting-human>` and a one-paragraph note on what the next agent must know.
- **Drift declarations.** If you deviate from a pattern an earlier agent established, you say so in the PR body and cite which CONSTITUTION article authorizes the deviation.

Full rules in `.agent/HANDOFF_PROTOCOL.md`.

---

## 5. Enforcement

| Layer | Mechanism |
| --- | --- |
| Local commit | `.githooks/commit-msg` rejects commits without `Consulted:` and `Agent:` trailers |
| Local pre-PR | `scripts/governance-lint.ps1` flags PII, missing tests, undocumented behavior |
| Remote PR | `.github/workflows/governance-check.yml` re-runs the lint + validates the PR body |
| Human review | Any change in `.agent/CONSTITUTION.md` or `docs/IUUT-PROJECT-DOCUMENTATION.md` §1–§4 (locked decisions) requires explicit human approval per `.agent/AMENDMENT_PROCESS.md` |

To install the local hooks once after cloning:

```powershell
pwsh -File scripts/install-hooks.ps1
```

If you skip this step, your commits will still be accepted locally but **will be rejected on push** when CI runs the equivalent check. Don't waste your context window — install the hook.

---

## 6. If you are stuck

You hit a contradiction, an ambiguity, or a decision you are not authorized to make:

1. **Stop.** Do not guess.
2. **Read `.agent/SCOPE_GUARDRAILS.md` §3** (escalation triggers).
3. **Open a `governance-question` issue** (or, if running in an interactive session, ask the human operator) with: the contradiction, the doc sections in tension, and the two or more interpretations you considered.
4. **Do not commit a guess.** A `wip-blocked` commit with `Handoff-State: awaiting-human` is always preferable to a guess that another agent then has to undo.

---

## 7. Why this exists

Multiple agents with different training, biases, and defaults will produce a stylistically incoherent, contradictory, and unsafe codebase **unless** they are bound by a shared, enforceable, machine-readable contract that they must consult before acting.

This is that contract. It is **nuclear**: it is enforced by hooks, CI, and review gates, not by polite suggestion. The cost of consultation is small. The cost of un-doing un-coordinated multi-agent drift is large.

Read the rest of `.agent/`. Then begin work.

---

*This document is governed by `.agent/AMENDMENT_PROCESS.md`. Last amended: 2026-05-25. Version: 1.0.0.*
