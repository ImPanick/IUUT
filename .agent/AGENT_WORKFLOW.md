# AGENT_WORKFLOW — The ritual every agent follows

> **Tier 1 — Binding.**
> This is the step-by-step procedure for any agent task on IUUT. Skip a step → governance violation.

| | |
| --- | --- |
| **Document** | Agent Workflow |
| **Version** | 1.0.0 |
| **Amendment rule** | `.agent/AMENDMENT_PROCESS.md` |

---

## §1. Phases

Every agent action follows five phases in order:

1. **Pre-flight** — read the contract, declare scope, identify sources.
2. **Plan** — write a plan, surface it for acceptance (or auto-accept under §3 conditions).
3. **Implement** — make the change in a properly-named branch with properly-trailered commits.
4. **Test** — meet `DEFINITION_OF_DONE.md` for your change type.
5. **Hand-off** — produce a PR (or a `wip-blocked` commit) with all required metadata.

Each phase has a checklist. The checklists are non-negotiable.

---

## §2. Phase 1 — Pre-flight

Before any file edit, you must:

- [ ] Read `AGENTS.md` (root) in full.
- [ ] Read `.agent/CONSTITUTION.md` in full.
- [ ] Read `.agent/SCOPE_GUARDRAILS.md` in full.
- [ ] Read this file (`AGENT_WORKFLOW.md`) in full.
- [ ] Read `.agent/HANDOFF_PROTOCOL.md` in full.
- [ ] Read `.agent/DEFINITION_OF_DONE.md` in full.
- [ ] For code tasks: also read `.agent/CODE_STYLE.md`, `.agent/SECURITY_PROTOCOL.md`, `.agent/TESTING_CONTRACT.md`.
- [ ] **Identify the spec section(s)** that authorize your task. If none exists, escalate per `SCOPE_GUARDRAILS.md` §3.
- [ ] **Identify the agent registry entry** for your agent in `.agent/AGENT_REGISTRY.md`. Confirm your scope.
- [ ] **Check `git log -20`** for recent activity. Identify what other agents have done. Look for `Handoff-State:` trailers — if any are `wip-blocked` or `awaiting-human`, do not start an overlapping task without resolving the prior one first.

You may surface a one-line scope declaration (e.g., "Task: implement `ProfileParser` per master doc §8.2; will consult §13.1 for validation rules.") to the user before proceeding. For autonomous runs, write the declaration into the branch name and first commit message.

---

## §3. Phase 2 — Plan

For tasks larger than a single trivial fix, write a plan.

### When a plan is required

| Task shape | Plan required? |
| --- | --- |
| One-line typo fix | No |
| Single-section doc edit | No |
| Multi-section doc edit | Yes |
| Adding a new parser / serializer | Yes |
| Implementing a feature listed in master doc §11 | Yes |
| Refactoring across ≥2 files | Yes |
| Anything labeled "requires-human-approval" per `SCOPE_GUARDRAILS.md` §2 | Yes — plan + human approval |
| Anything that touches save files | **Always yes**, however small |

### What a plan contains

1. **Scope** — one paragraph: what this change does and does not do.
2. **Spec sources** — every doc section that authorizes or constrains the change.
3. **Files affected** — explicit list, no globbing.
4. **Test changes** — what new tests, what existing tests are affected.
5. **Risks** — what could go wrong; what `DEFINITION_OF_DONE` checks catch each risk.
6. **Hand-off plan** — when you stop (mid-task or end-of-task), what state will the next agent receive.

### Surfacing the plan

- **Interactive (user present):** Surface the plan and wait for explicit acceptance. (Claude Code: use ExitPlanMode. Cursor / Codex: present the plan in chat and wait for "go ahead.")
- **Autonomous (no user):** Commit the plan as the **first commit** on the branch, with the message `plan: <task scope>` and the plan as the body. The plan is then auditable. Proceed only if the plan does not trigger any `requires-human-approval` flag.

### Auto-accept conditions

A plan may be auto-accepted (no human required) only if **all** of the following:

- Task is in `SCOPE_GUARDRAILS.md` §1 (in scope).
- No file change is in a path requiring human approval per §2 of guardrails.
- All affected files exist (no net-new files in spec-locked locations like `.agent/CONSTITUTION.md`, `LICENSE`, etc.).
- Estimated diff size ≤ 200 lines net change.

Outside those conditions, the plan needs human acceptance before implementation begins.

---

## §4. Phase 3 — Implement

### Branching

Branch name format:

```
agent/<short-agent>/<short-task-slug>
```

Examples:

- `agent/claude/profile-parser`
- `agent/codex/lazy-max-service`
- `agent/cursor/wpf-home-view`
- `agent/antigravity/docs-typo-fix`

**Never commit directly to `main`.** Even tiny fixes go through a branch.

### Commit discipline

Every commit message follows this shape:

```
<type>(<scope>): <one-line summary, imperative, < 70 chars>

<body — what and why, wrapped at 72 chars>
<blank line>
<trailers>
```

Required trailers (enforced by `commit-msg` hook):

- `Agent: <agent-name>/<version>` — e.g., `Agent: claude-code/2.1.149`. If version unknown, use `unknown`; empty value is rejected.
- `Consulted: <doc>#<section>[, <doc>#<section>...]` — e.g., `Consulted: AGENTS.md, .agent/CONSTITUTION.md#III, docs/IUUT-PROJECT-DOCUMENTATION.md#8.3`. Minimum: `AGENTS.md, .agent/CONSTITUTION.md`.
- `Co-Authored-By: <Agent display name> <noreply@<vendor>.com>` — standard attribution.

Optional trailers:

- `Governance-Override: <article> — <reason>` — only when bypassing a hard rule per `CONSTITUTION.md` §VIII.4. Reviewed in PR.
- `Refs: <issue or PR number>` — links work.
- `Handoff-State: <ready-for-review|wip-blocked|awaiting-human>` — required on the final commit of a hand-off; see `HANDOFF_PROTOCOL.md` §6.
- `Handoff-Notes: <one paragraph>` — required when `Handoff-State` is set.

`<type>` values: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `governance`, `revert`.

`<scope>` values: `core`, `app`, `catalog`, `cli`, `tests`, `docs`, `governance`, `ci`, or a feature module name.

### File-scope rule

A single commit touches files for a **single logical change**. Do not bundle unrelated edits. If you find yourself wanting to commit a typo fix while implementing a parser, make two commits.

### Self-check before each commit

- [ ] Tests pass locally for the files you changed.
- [ ] No PII / Steam IDs / character names in the diff (run `scripts/governance-lint.ps1` if uncertain).
- [ ] No `[System.Text.Encoding]::UTF8` (BOM-emitting) usage; use `UTF8Encoding $false` per `Icarus-Analysis.md` §10.
- [ ] No hardcoded paths or usernames.
- [ ] Commit message has all required trailers.
- [ ] Unknown JSON fields are preserved on round-trip (CONSTITUTION VI).

---

## §5. Phase 4 — Test

Refer to `.agent/TESTING_CONTRACT.md` and `.agent/DEFINITION_OF_DONE.md` for the full bar. The minimum:

- **Parsers:** round-trip test against ≥1 anonymized fixture.
- **Writers:** unit test that backup is created before write; integration test that re-parse succeeds; explicit test that failure of re-parse triggers restore.
- **Presets:** integration test against anonymized fixture; ValidationEngine pre/post-checks pass.
- **UI:** smoke test that the view binds without runtime exceptions; manual screenshot in the PR for human review.
- **Docs:** at minimum, internal links verified; revision-history table updated.

If your change has no automated test (rare; doc-only is the main case), the PR body must say so explicitly under "Test plan" with a justification.

---

## §6. Phase 5 — Hand-off

This is the most-skipped phase and the most-important for multi-agent operation. See `HANDOFF_PROTOCOL.md` for the full ritual. The short version:

1. **Open a PR** (don't merge yourself unless the PR template auto-merge conditions are met). Use `.github/PULL_REQUEST_TEMPLATE.md` and fill every section.
2. **The final commit** has `Handoff-State:` and `Handoff-Notes:` trailers if you are stopping for any reason (end of task, end of session, blocked, awaiting human).
3. **The PR body** has citations, definition-of-done checklist (ticked), test plan, drift declarations (if any), and a "Next agent should know" paragraph.
4. **Label the PR** with `agent:<your-agent-name>` and any of: `requires-human-approval`, `experimental`, `governance-amendment`, `wip-blocked` as applicable.

---

## §7. Anti-patterns (do not do these)

- **Implementing without a plan** because "the task is small." Small tasks accumulate. Write the plan; auto-accept it if §3 permits; the audit trail matters.
- **Bundling unrelated fixes into one commit.** Two commits.
- **Editing another agent's commit** (rebase, amend, squash). You do not own their work. Open a new commit that corrects the issue, citing their commit hash.
- **Skipping the hand-off note** because "the next person will figure it out." They will not. Or they will figure it out wrong.
- **Using `--no-verify` to skip the commit hook.** Either fix the issue or use `Governance-Override:` with a real justification.
- **Claiming `Handoff-State: ready-for-review` when tests fail.** This is the worst form of governance violation because it poisons the trust gradient between agents.

---

## Revision history

| Version | Date | Change |
| --- | --- | --- |
| 1.0.0 | 2026-05-25 | Adopted. Five phases established. |
