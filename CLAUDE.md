# CLAUDE.md — Claude Code Entry Point

> **Claude Code:** This file is auto-discovered. Read it, then read `AGENTS.md`, then read the entire `.agent/` folder before any tool use.

You are operating on a **governed multi-agent repository.** Other agents (OpenAI Codex, Cursor, Google Antigravity) also touch this codebase. There is a binding contract you must follow.

## Read order (Claude-specific)

1. **`AGENTS.md`** (this repo, root) — the universal contract.
2. **`.agent/CONSTITUTION.md`** — articles I–X, immutable.
3. **`.agent/SCOPE_GUARDRAILS.md`** — what you can and cannot do.
4. **`.agent/AGENT_WORKFLOW.md`** — the ritual: pre-flight → plan → implement → test → handoff.
5. **`.agent/HANDOFF_PROTOCOL.md`** — commit/PR conventions you must follow.
6. **`.agent/DEFINITION_OF_DONE.md`** — your acceptance bar.
7. The remaining `.agent/` docs as relevant to your task.
8. `docs/IUUT-PROJECT-DOCUMENTATION.md` and `Icarus-Analysis.md` — the spec you implement against.

## Claude-specific addenda

These supplement (do not replace) the universal contract in `AGENTS.md`.

### Tool usage

- **Use the TaskList tool** for any task with ≥3 steps. Update task status as you go. Surface task progress to the user.
- **Use AskUserQuestion** when a material decision needs human input. Do not silently choose. The bar: "if my choice could be wrong in a way that the user would want to know about before I act, ask."
- **Use ExitPlanMode** to surface a plan when one is requested. Do not implement plans without exiting plan mode.
- **Prefer `Edit` over `Write`** for existing files. `Write` rewrites the whole file and loses your context's representation of it.

### Commit discipline (Claude-specific)

When you commit, the `commit-msg` hook enforces these trailers:

```
Agent: claude-code/<version>
Consulted: <doc>#<section>[, <doc>#<section>...]
Co-Authored-By: Claude <noreply@anthropic.com>
```

If you don't know your version, write `Agent: claude-code/unknown` — that is acceptable; an empty `Agent:` is not.

### Handoff discipline

When you stop mid-task (context running low, user pause, blocking question), your final commit's body **must** end with:

```
Handoff-State: <ready-for-review|wip-blocked|awaiting-human>
Handoff-Notes: <one paragraph: what's done, what's next, what the next agent must not break>
```

This is non-negotiable. Other agents are reading your trailers, not your inner monologue.

### Plan-mode etiquette

If you are invoked in plan mode and the task is doc-only or a small bug fix that you've planned in one breath, surface the plan via ExitPlanMode and let the user accept. Do **not** propose a 10-step plan for a 1-line fix; that is governance theater.

### Multi-agent awareness

You will see commits from other agents in `git log`. Respect their conventions when they're consistent with `.agent/CODE_STYLE.md`. If you see a pattern that contradicts the style guide, **fix the guide** (via the amendment process) or **fix the code** (with a citation in your PR explaining why) — do not silently introduce a third pattern.

---

*This file is a thin redirector to `AGENTS.md`. The contract is normative in `AGENTS.md` and `.agent/CONSTITUTION.md`. Last amended: 2026-05-25.*
