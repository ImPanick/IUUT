# Contributing to IUUT

Thank you for considering a contribution. **IUUT is a governed multi-agent
repository** — both human and AI contributors operate under a binding contract.
This is not optional ceremony; it is enforced by git hooks and CI.

## Read this first (in order)

1. **[`AGENTS.md`](AGENTS.md)** — the universal contract. Start here.
2. **[`.agent/CONSTITUTION.md`](.agent/CONSTITUTION.md)** — the immutable principles.
3. **[`.agent/SCOPE_GUARDRAILS.md`](.agent/SCOPE_GUARDRAILS.md)** — what's in / out of scope.
4. **[`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md)** — how to set up, build, test, and run.
5. **[`.agent/AGENT_WORKFLOW.md`](.agent/AGENT_WORKFLOW.md)** — the change ritual.

If you are an AI agent, your IDE-specific redirector (`CLAUDE.md`, `.cursorrules`,
`.cursor/rules/agents.mdc`, `.antigravity/rules.md`) points you at the same contract.

## Quick start

```powershell
git clone https://github.com/ImPanick/IUUT.git
cd IUUT
pwsh -File scripts/install-hooks.ps1     # REQUIRED — installs the commit-msg gate
dotnet build IcarusUltimateUtilityTool.sln
dotnet test  IcarusUltimateUtilityTool.sln
```

## The contribution loop

1. **Find a spec authorization.** Every change cites the `docs/IUUT-PROJECT-DOCUMENTATION.md`
   (or `Icarus-Analysis.md`) section that authorizes it. No spec authorization → propose
   a spec change first, or open a `governance-question` issue.
2. **Branch:** `agent/<agent-or-handle>/<task-slug>` (e.g. `agent/human-jane/profile-parser`).
   Never commit directly to `main`.
3. **Commit** with the mandatory trailers (the `commit-msg` hook enforces them):
   ```
   Agent: <name>/<version>            # e.g. human-jane/n-a, claude-code/2.1.149
   Consulted: AGENTS.md, .agent/CONSTITUTION.md, <spec sections>
   Co-Authored-By: <Name> <email>
   ```
4. **Test** to the bar in [`.agent/DEFINITION_OF_DONE.md`](.agent/DEFINITION_OF_DONE.md).
5. **Open a PR** filling every section of the [PR template](.github/PULL_REQUEST_TEMPLATE.md).
   CI (Governance Check + Build & Test) must be green.

## The hard rules (summary)

**You must:** cite consulted sections in commits and PRs; round-trip every parser
change against a fixture; back up before writing save files and restore on failure;
anonymize all fixtures; preserve unknown JSON fields.

**You must not:** add telemetry / cloud upload / auth / un-enumerated network calls;
hardcode usernames / paths / Steam IDs / character names; bypass the commit hook
without a justified `Governance-Override:`; amend `.agent/CONSTITUTION.md` outside the
[amendment process](.agent/AMENDMENT_PROCESS.md); mark a task done when tests fail.

Full normative text: `.agent/CONSTITUTION.md`.

## Reporting issues

- **Bug / feature / catalog update:** use the matching [issue template](.github/ISSUE_TEMPLATE).
- **A contradiction or ambiguity in the spec/governance:** open a `governance-question`
  issue. Do not guess — see `.agent/SCOPE_GUARDRAILS.md` §3.
- **A security or privacy concern (e.g. leaked PII):** see [`SECURITY.md`](SECURITY.md).

## Code of conduct

Be excellent to each other. This is a community tool built by volunteers and agents.
Coordinate before editing shared prospect files in multiplayer contexts. Never commit
real Steam IDs or other people's data.

## License

By contributing, you agree your contributions are licensed under the project's
[MIT License](LICENSE).
