# HANDOFF_PROTOCOL — Multi-agent commit, PR, and hand-off conventions

> **Tier 1 — Binding.**
> When multiple agents touch a repo without explicit hand-off conventions, you get drift, duplicated work, and lost context. This document is the mechanical solution.

| | |
| --- | --- |
| **Document** | Hand-off Protocol |
| **Version** | 1.0.0 |
| **Amendment rule** | `.agent/AMENDMENT_PROCESS.md` |

---

## §1. Branch naming

```
agent/<short-agent>/<short-task-slug>
```

- `<short-agent>` ∈ {`claude`, `codex`, `cursor`, `antigravity`, `human`}. New agents are added to this list via `AMENDMENT_PROCESS.md`.
- `<short-task-slug>` is kebab-case, ≤ 40 chars, descriptive of intent: `profile-parser`, `lazy-max-service`, `flags-dat-hex-test`, `docs-correctness-pass`.

Reserved prefixes:
- `release/<vX.Y.Z>` — release branches (humans only).
- `governance/<short-description>` — amendments to `.agent/` (requires human approval).
- `hotfix/<short-description>` — emergency fixes (requires `CONSTITUTION` IX rationale in PR body).

`main` is the trunk. Direct commits to `main` are rejected (branch protection).

---

## §2. Commit message anatomy

```
<type>(<scope>): <one-line subject, imperative, ≤ 70 chars>

<body, wrapped at 72 chars>
<can be multiple paragraphs>

<empty line>

Agent: <agent-name>/<version>
Consulted: <doc>#<section>[, <doc>#<section>...]
Co-Authored-By: <Display Name> <noreply@<vendor>.com>
[optional trailers...]
```

### Mandatory trailers (enforced by `.githooks/commit-msg`)

| Trailer | Format | Required? | Example |
| --- | --- | --- | --- |
| `Agent:` | `<name>/<version>` | Always | `Agent: claude-code/2.1.149` |
| `Consulted:` | comma-separated `<doc>#<section>` | Always | `Consulted: AGENTS.md, .agent/CONSTITUTION.md#III, docs/IUUT-PROJECT-DOCUMENTATION.md#12.1` |
| `Co-Authored-By:` | `Name <email>` | Always | `Co-Authored-By: Claude <noreply@anthropic.com>` |

### Optional trailers

| Trailer | When | Example |
| --- | --- | --- |
| `Governance-Override:` | When bypassing a hard rule (CONSTITUTION VIII.4) | `Governance-Override: V — hot-fix for save corruption affecting users, doc amendment to follow` |
| `Refs:` | Linking issue / PR | `Refs: #42` |
| `Handoff-State:` | Required on final commit before pause/PR | See §6 |
| `Handoff-Notes:` | Required when `Handoff-State` set | See §6 |
| `Reviewed-By:` | Added during PR review | `Reviewed-By: human-josep` |
| `Verified-Against-Save:` | When change is verified against a live save | `Verified-Against-Save: %LOCALAPPDATA%\Icarus\Saved\PlayerData\<scrubbed-id>\ (2026-05-25)` |

### `<type>` values

`feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `governance`, `revert`.

### `<scope>` values

`core`, `app`, `catalog`, `cli`, `tests`, `docs`, `governance`, `ci`, or a feature module name (e.g., `profile`, `characters`, `lazy-max`).

### Examples

**Good:**

```
feat(core): add ProfileParser with MetaResources round-trip

Implements Profile.json parsing per master doc §8.2. MetaResources
round-trip preserves unknown MetaRow keys via System.Text.Json
extension data, satisfying CONSTITUTION VI (forward compat).
Validates UserID against folder name per §13.1; raises on mismatch.

Agent: claude-code/2.1.149
Consulted: AGENTS.md, .agent/CONSTITUTION.md#III,#VI,#VIII,
 docs/IUUT-PROJECT-DOCUMENTATION.md#8.2,#13.1, Icarus-Analysis.md#3
Co-Authored-By: Claude <noreply@anthropic.com>
```

**Rejected by hook (missing `Consulted:`):**

```
feat(core): add ProfileParser

Agent: claude-code/2.1.149
Co-Authored-By: Claude <noreply@anthropic.com>
```

**Rejected by hook (missing `Agent:`):**

```
fix(docs): typo

Consulted: AGENTS.md, .agent/CONSTITUTION.md
Co-Authored-By: Claude <noreply@anthropic.com>
```

---

## §3. Pull request anatomy

Every PR uses the template at `.github/PULL_REQUEST_TEMPLATE.md`. CI rejects PRs whose bodies do not have all template sections filled.

Required sections:

1. **Summary** — what this PR does, in 1–3 bullets.
2. **Spec authorization** — which spec section(s) authorize this change.
3. **Consultation** — list of docs read; minimum includes `AGENTS.md` and `.agent/CONSTITUTION.md`.
4. **Files touched** — explicit list (no globs).
5. **Test plan** — what tests run; manual verification steps if relevant.
6. **Definition-of-Done checklist** — ticked items per `DEFINITION_OF_DONE.md`.
7. **Drift declarations** — anything done differently from prior agents' work or from the spec; cite the CONSTITUTION article authorizing the drift.
8. **Hand-off notes** — what the reviewer / next agent needs to know.

Required PR labels:

- `agent:<your-agent-name>` — one per PR.
- One of: `ready-for-review`, `wip-blocked`, `experimental`, `governance-amendment`, `requires-human-approval`.

---

## §4. Multi-agent concurrency rules

Multiple agents may have open branches simultaneously. To avoid stomping:

1. **One agent per branch.** Branch names encode the agent. Do not work on another agent's branch unless you are explicitly resolving a `wip-blocked` hand-off (see §7).
2. **Lock by spec section, not by file.** If your task implements master doc §8.2 (Profile parser), you "own" that spec section until your PR merges or you publish a `wip-blocked` hand-off. Another agent that wants to work on §8.2 sees the active branch and either waits, or coordinates via a GitHub issue.
3. **Cross-cutting changes** (e.g., refactoring `IUUT.Core` API surface) are tagged `cross-cutting` and explicitly block other agents from starting cross-cutting work until merged.
4. **No two agents work on the same file** simultaneously, even on different features. If a file needs work in two directions, the second agent waits.

To check what other agents are doing:

```bash
git log --all --oneline --decorate | head -20
git branch -r | grep '^  origin/agent/'
```

---

## §5. Drift detection and declaration

When you notice you are about to deviate from a pattern an earlier agent established:

1. **Look it up.** Search for the pattern: `git log --all --grep=<related-term>`, or `grep -rn` for the code construct.
2. **Decide:** Is the earlier pattern wrong (in violation of `CODE_STYLE.md` or `CONSTITUTION.md`), or just different?
3. **If wrong:** propose a fix to the earlier code in your PR. Cite the rule. Tag the PR `drift-correction`.
4. **If just different:** either adopt the earlier pattern, or open a `CODE_STYLE.md` amendment to formalize the new pattern. Do **not** silently introduce a third pattern (CONSTITUTION X.6).
5. **Declare in PR body** under "Drift declarations": pattern name, earlier example (file:line), your deviation, and the CONSTITUTION/CODE_STYLE article authorizing the change.

---

## §6. Hand-off ritual

A "hand-off" is any moment work pauses where another agent (or the same agent in a future session) might pick it up. End-of-task hand-offs go via PR. Mid-task hand-offs go via a `wip-blocked` commit on the branch.

### Required trailers on the final commit before any hand-off

```
Handoff-State: <state>
Handoff-Notes: <one paragraph>
```

`<state>` values:

| State | Meaning |
| --- | --- |
| `ready-for-review` | Task is complete per `DEFINITION_OF_DONE.md`; opening / updating PR. |
| `wip-blocked` | Cannot proceed; needs decision, info, or another agent's work to land first. |
| `awaiting-human` | Needs human approval / clarification per `SCOPE_GUARDRAILS.md` §3. |
| `paused` | Out of session time / context budget; will resume; no blocker. |

`Handoff-Notes` is a single paragraph (≤ 4 sentences). Required content:

- What is **done** (last verified state).
- What is **next** (the immediate next step, not the whole task plan).
- What the next agent must **not break** (any subtle invariant your changes rely on).

### Example

```
refactor(core): extract ProfileMetaResourceSerializer

[body explaining what / why]

Agent: claude-code/2.1.149
Consulted: AGENTS.md, .agent/CONSTITUTION.md, docs/IUUT-PROJECT-DOCUMENTATION.md#8.2
Co-Authored-By: Claude <noreply@anthropic.com>
Handoff-State: wip-blocked
Handoff-Notes: Extracted the serializer cleanly; unit tests pass. Blocked
 on a decision: should the round-trip test include Exotic_Uranium (now
 verified on live save per Icarus-Analysis §3.2) as a regression guard, or
 stay catalog-driven? Awaiting human input. Do not merge until resolved;
 the no-op handler stub on line 142 must be replaced before any production
 use — it currently returns `default(int)` for unknown MetaRow keys, which
 violates Article VI.
```

---

## §7. Resuming someone else's hand-off

If you are picking up another agent's `wip-blocked` or `paused` branch:

1. **Read every commit on that branch** from the merge-base forward. Do not skim.
2. **Read the final commit's `Handoff-Notes:`** in full. That is the briefing.
3. **If the hand-off state is `awaiting-human` and the human has now responded:** add a commit on the same branch that links the human's resolution (`Refs: <issue#>` or quote the chat) and proceeds.
4. **If the hand-off state is `wip-blocked` and the blocker has cleared:** add a commit that documents what cleared the blocker.
5. **If you cannot tell what the blocker was:** escalate per `SCOPE_GUARDRAILS.md` §3. Do not guess.
6. **Update the `Agent:` trailer** on your commits to your own agent identity — do not impersonate the previous agent.
7. **Optionally, your first commit may have a `Resumed-From: <commit-sha>` trailer** pointing at the prior agent's final commit, for auditability.

---

## §8. Reverts and rollbacks

If you must revert another agent's commit:

1. Use `git revert <sha>` to create an inverse commit (do not delete history).
2. Commit message: `revert(<scope>): revert <subject of reverted commit>`.
3. Body must explain **why** the revert is needed and cite the CONSTITUTION article / spec section that the original violated.
4. Trailers include `Reverts: <sha>` in addition to the mandatory ones.
5. Open a PR; do not push the revert directly to `main`.

---

## §9. Tags and releases

Releases are tagged `vX.Y.Z` per SemVer. Tag creation is human-only. Agents propose release readiness via a `release-readiness` issue or PR; they do not tag.

---

## Revision history

| Version | Date | Change |
| --- | --- | --- |
| 1.0.0 | 2026-05-25 | Adopted. Branch / commit / PR / hand-off conventions established. |
