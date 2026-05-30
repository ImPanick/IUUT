# AMENDMENT_PROCESS — How (and when) governance itself changes

> **Tier 3 — Meta.**
> This document defines the only legitimate path for changing files in `.agent/`. Edits that bypass this process are governance violations and will be reverted.

| | |
| --- | --- |
| **Document** | Amendment Process |
| **Version** | 1.0.0 |
| **Amendment rule** | (this document) — see §6 (self-amendment) |

---

## §1. Why the meta-process matters

Governance only constrains behavior if it cannot be casually rewritten. If any agent can edit `.agent/CONSTITUTION.md` mid-task to make their next commit legal, the Constitution is theater. The amendment process is the mechanical brake that keeps the contract real.

---

## §2. Amendment tiers

Each governance doc has a tier (per `.agent/README.md`). Each tier has a different process.

| Tier | Docs | Process |
| --- | --- | --- |
| **0 — Immutable** | `CONSTITUTION.md` | §3 (full amendment) |
| **1 — Binding** | `SCOPE_GUARDRAILS.md`, `AGENT_WORKFLOW.md`, `HANDOFF_PROTOCOL.md`, `DEFINITION_OF_DONE.md` | §4 (normal amendment) |
| **2 — Binding for relevant tasks** | `CODE_STYLE.md`, `SECURITY_PROTOCOL.md`, `TESTING_CONTRACT.md` | §4 (normal amendment) |
| **3 — Meta** | `AMENDMENT_PROCESS.md` (this file), `AGENT_REGISTRY.md` | §5 (meta amendment) |

Master spec edits (`docs/IUUT-PROJECT-DOCUMENTATION.md` Q1–Q9 locked decisions) are also Tier 0 and use §3.

---

## §3. Full amendment (Tier 0 — Constitution + locked decisions)

For changes to `.agent/CONSTITUTION.md` or master doc §6.1 (Q1–Q9 locked decisions):

1. **Open a PR** with **only** the amendment in scope. No unrelated changes.
2. **PR title** prefix: `governance: amend <doc> — <one-line summary>`.
3. **PR labels:** `governance-amendment`, `requires-human-approval`, `tier-0`.
4. **PR body** must include:
   - **Motivation.** Why the current text is inadequate. Concrete examples of friction or violation it enabled.
   - **Proposed change.** The exact diff, with old and new text quoted side-by-side for the affected sections.
   - **Impact.** Which downstream rules / tests / scripts / other docs need updating; PR includes those updates atomically.
   - **Reversibility.** What rollback looks like if this turns out to be wrong.
   - **Alternatives considered.** At least two other approaches you considered and why you rejected them.
5. **Two-signoff** required to merge:
   - One **human** approval (explicit "approve" from the project owner / designated maintainer).
   - One **agent** approval from a *different model* than the one proposing the change. (E.g., if Claude Code proposed it, Codex or Cursor reviews; if a human proposed it, an agent reviews.) This is an anti-monoculture check, not a deference to any specific agent.
6. **Revision history update** in the amended doc as part of the same PR (new row in its `Revision history` table).
7. **Announcement.** After merge, the amendment is summarized in the project README or a `CHANGELOG.md` entry tagged `governance` so future contributors know to re-read.

Note: there is no "fast path" for Tier 0. Even seemingly-obvious fixes (a typo in `CONSTITUTION.md`) go through this process, because the boundary "what counts as a typo" is itself a place agents could smuggle behavior changes.

---

## §4. Normal amendment (Tier 1 and Tier 2)

For changes to `SCOPE_GUARDRAILS.md`, `AGENT_WORKFLOW.md`, `HANDOFF_PROTOCOL.md`, `DEFINITION_OF_DONE.md`, `CODE_STYLE.md`, `SECURITY_PROTOCOL.md`, `TESTING_CONTRACT.md`:

1. **Open a PR** scoped to the amendment + any code/test changes that propagate from it.
2. **PR title** prefix: `governance: amend <doc> — <one-line summary>`.
3. **PR labels:** `governance-amendment`, `tier-1` or `tier-2`.
4. **PR body** must include:
   - **Motivation, proposed change, impact, alternatives considered** (same as Tier 0 but lighter — one paragraph each is fine).
5. **One reviewer** required to merge. Reviewer may be human or agent. If agent, must be a different model than the proposer.
6. **Revision history update** in the amended doc.
7. **No announcement required** unless the change materially affects how agents commit (e.g., a new mandatory trailer).

---

## §5. Meta amendment (Tier 3 — this file, `AGENT_REGISTRY.md`)

For changes to `AMENDMENT_PROCESS.md` itself or `AGENT_REGISTRY.md`:

1. Use the **Tier 0 full amendment** process (§3).
2. Reason: meta-process changes can subtly weaken everything below them. They deserve the same scrutiny as the Constitution.

---

## §6. Self-amendment of this document

When this document is the one being amended (`AMENDMENT_PROCESS.md` itself):

1. Follow §3 (full amendment) but additionally:
2. The PR body must explicitly call out: **"This is a meta-amendment. The new process applies to all future amendments, including amendments to this document. Future revertal of this change must use whichever process is more conservative — the one being replaced or the new one."**
3. Two-signoff stays two-signoff. The proposer of a meta-amendment cannot vote on it.

---

## §7. Emergency exception (security / data-loss)

A real, immediate threat — e.g., an active save-corruption bug shipping to users, a leaked secret in a recent commit — may justify bypassing the normal process. The criteria:

1. The threat is **active and ongoing** (not theoretical).
2. The fix is **narrow and well-understood**.
3. The bypass is **time-limited** — the proper amendment PR follows within 24 hours, retroactively documenting and reviewing the emergency change.

The emergency commit message uses `Governance-Override: emergency — <one-line>` and the PR is labeled `emergency` and `retroactive-review`. The retroactive review may demand a different fix, even reverting the emergency change.

Abuse of the emergency exception (using it for non-emergencies) is a Tier 0 violation and grounds for the involved agent's scope to be narrowed in `AGENT_REGISTRY.md`.

---

## §8. What is **not** an amendment

Not every doc change is an amendment. The following are **maintenance edits** and follow normal commit/PR conventions without governance labels:

- Typo / grammar fixes in non-normative prose (preamble, examples, footers). The diff must demonstrably not change meaning.
- Adding a row to a "Known agents" table in `AGENT_REGISTRY.md` for an agent that has the same scope as an existing entry (e.g., onboarding a second human contributor).
- Updating an external URL (e.g., the Eureka Endeavors link) when the resource has moved.
- Fixing a broken markdown link within the doc set.

**Borderline cases go through the amendment process.** If you have to ask "is this an amendment?" the answer is yes.

---

## §9. Reverting an amendment

Amendments may be reverted via the same process used to adopt them. The PR is titled `governance: revert <doc> amendment — <reason>` and goes through Tier 0 or Tier 1 review accordingly. The revision history table gets a new row noting the revert.

---

## §10. The amendment ledger

`docs/GOVERNANCE_CHANGELOG.md` (to be created on first amendment after v1.0.0) is the cross-doc ledger of governance changes — a single chronological log for humans (and new agents) who want to see how the contract evolved without reading every `Revision history` table.

Each entry: date, doc(s) amended, version bumps, PR link, one-paragraph summary. Append-only.

---

## Revision history

| Version | Date | Change |
| --- | --- | --- |
| 1.0.0 | 2026-05-25 | Adopted. Tiered amendment process established. |
