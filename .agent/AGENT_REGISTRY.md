# AGENT_REGISTRY — Known agents, scopes, and onboarding

> **Tier 3 — Meta.**
> This is the registry of AI agents authorized to operate on this repository, with their respective scopes and constraints. Changes to this file follow `AMENDMENT_PROCESS.md` §5 (Tier 0 full amendment).

| | |
| --- | --- |
| **Document** | Agent Registry |
| **Version** | 1.0.0 |
| **Amendment rule** | `.agent/AMENDMENT_PROCESS.md` §5 |

---

## §1. Registered agents (v1.0.0)

| Agent | Identifier | Tier | Scope | Notes |
| --- | --- | --- | --- | --- |
| **Claude Code** (Anthropic) | `claude-code` | **Primary** | Full | Authoritative for the spec; uses TaskList for multi-step tracking; uses AskUserQuestion for material decisions. May propose Tier 0 amendments. |
| **OpenAI Codex** | `codex` / `codex-cli` | Secondary | Full | Auto-discovers `AGENTS.md`. May implement code in any module per `AGENT_WORKFLOW.md`. May not initiate Tier 0 amendments without prior coordination. |
| **Cursor** | `cursor` | Interactive | Full (user-driven) | Operates with a human in the loop. Inherits human-judgment latitude on edge cases but still must trail-cite. |
| **Google Antigravity** | `antigravity` | **Experimental** | Restricted (see §3) | Newer agent in this codebase; scope-limited until track record accumulates. |
| **Human contributors** | `human-<github-handle>` | Owner | Full + governance | May approve Tier 0 amendments. Should still author commits with `Agent: human-<handle>` trailer for audit consistency. |

### Identifier discipline

Every commit's `Agent:` trailer uses one of the identifiers in column 2, suffixed with `/<version>` (or `/unknown`). Inventing new identifiers is a governance violation; add the agent here first.

---

## §2. Scope levels

| Level | What's permitted | Examples of agents at this level |
| --- | --- | --- |
| **Full** | All non-`requires-human-approval` work per `SCOPE_GUARDRAILS.md` §1; may propose amendments at any tier (Tier 0 still needs two-signoff per `AMENDMENT_PROCESS.md`). | Claude Code, Codex, Cursor, human contributors |
| **Restricted** | Doc-only changes; refactors in non-safety-critical modules; may NOT write to save-file mutators, ProspectBlob codecs, ValidationEngine pre-checks, Steam API integration, or anything else listed in §3 without explicit per-PR human approval. | Antigravity (currently) |
| **Read-only** | May read, may comment on PRs, may propose changes via issues. May NOT commit. | Reserved for trial / evaluation of new agents before they're granted Restricted or Full. |
| **Owner** | Everything Full can do, plus: approve Tier 0 amendments, revoke other agents' scope, tag releases, push directly to release branches in emergencies (still with `Governance-Override:` trailer). | Human project owner(s). |

Scope changes for an agent require a Tier 0 amendment (§5 of AMENDMENT_PROCESS).

---

## §3. Restricted-scope exclusion list (Antigravity specifically)

Until Antigravity's classification is upgraded via amendment, the following are off-limits without explicit per-PR human approval (label `antigravity-elevated` + reviewer sign-off):

- `IUUT.Core/Services/BackupManager.cs` (or successor)
- Any class implementing `ISaveFileMutator` (or equivalent)
- `IUUT.Core/ProspectBlob/**` (codec, hash, Adler-32, FProperty)
- `IUUT.Core/Validation/ValidationEngine.cs` and its rules
- `IUUT.Core/Services/SteamProfileResolverService.cs` (only outbound network)
- `.agent/**` (governance amendments)
- `.githooks/`, `.github/workflows/`, `scripts/governance-lint.ps1` (enforcement plumbing)
- `LICENSE`, `*.csproj`, `*.sln`, `Directory.Build.props`

The list is restrictive on purpose: it covers everything where a subtle bug could corrupt user data, leak PII, or weaken the governance gate. As Antigravity demonstrates safe operation on lower-stakes changes, items are removed via amendment.

---

## §4. Adding a new agent

To onboard a new AI agent (Aider, Cline, Windsurf, Devin, future tools):

1. **Open a Tier 0 amendment PR** (per `AMENDMENT_PROCESS.md` §3) that:
   - Adds a row to §1 with proposed identifier, tier, scope.
   - Adds an onboarding redirector file at the agent's discovery path (e.g., `.aider.conf.yml`, `.cline/rules.md`).
   - Documents which model(s) the agent runs and any vendor-specific safety considerations.
2. **Run an onboarding test:** the new agent makes a no-op PR (e.g., a fixture README typo fix) demonstrating it can:
   - Read `AGENTS.md` and `.agent/` per the reading order.
   - Produce a commit with all required trailers.
   - Open a PR with the template fully filled.
3. **Two-signoff for the amendment**, including review of the onboarding test PR.
4. **Initial scope** for any new agent is **Restricted** unless a strong justification for Full is presented and accepted.

---

## §5. Removing or narrowing an agent's scope

If an agent demonstrates a pattern of governance violations (false `ready-for-review` claims, PII leaks, bypassed hooks, ignored scope), its scope may be narrowed:

1. **Open a Tier 0 amendment PR** titled `governance: narrow scope of <agent> — <reason>`.
2. **Cite the violations** with commit / PR references.
3. **Propose the new scope** (Restricted, Read-only, or removed entirely).
4. **Two-signoff** as normal.

This is not punitive; it's calibration. An agent's scope reflects its demonstrated reliability on this codebase. Scope can also be **broadened** the same way once track record warrants it.

---

## §6. Vendor / model rotation

When a vendor releases a new model version (e.g., Claude 4.7 → 4.8), the `<version>` in the `Agent:` trailer changes automatically. No registry update needed for routine version bumps.

Significant generational changes (Claude 4 → Claude 5, GPT-4 → GPT-5) **may** be cause for re-evaluating scope if the model's behavior in this codebase changes materially. Amendment PR if so.

---

## §7. Coexistence with humans

Human contributors are agents too, for governance purposes. Same commit trailers (`Agent: human-<github-handle>`), same PR template, same `DEFINITION_OF_DONE.md` bar.

The exception: humans may approve Tier 0 amendments and resolve `awaiting-human` hand-offs. AI agents may not (an AI agent's review counts as the second-signoff if it's a different model than the proposer, but the human signoff is structurally required for Tier 0).

---

## §8. Agent self-identification

Every agent's first commit on a new branch should include in the commit body (not the trailer) a brief self-identification:

```
Agent self-id: claude-code v2.1.149 operating in interactive mode for user josep.
Session started 2026-05-25T13:45:00Z. Reading order completed: AGENTS.md,
.agent/CONSTITUTION.md, .agent/SCOPE_GUARDRAILS.md, .agent/AGENT_WORKFLOW.md,
.agent/HANDOFF_PROTOCOL.md, .agent/DEFINITION_OF_DONE.md.
```

This is optional but recommended; it gives future agents and humans context on the session that produced the branch.

---

## Revision history

| Version | Date | Change |
| --- | --- | --- |
| 1.0.0 | 2026-05-25 | Adopted. Initial registrants: Claude Code (Primary), Codex (Secondary), Cursor (Interactive), Antigravity (Experimental + Restricted). |
