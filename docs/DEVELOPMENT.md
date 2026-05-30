# DEVELOPMENT — Local developer runbook

> The day-to-day "how do I actually work on IUUT" guide. For the *contract* that
> governs how changes are made, read `AGENTS.md` and `.agent/` first — this doc
> assumes you have. For the CI/CD pipeline and release process, see
> [`docs/CICD.md`](CICD.md).

| | |
| --- | --- |
| **Audience** | Humans and AI agents writing code for IUUT |
| **Status** | Active from ground-breaking (2026-05-25) |
| **Authority** | `docs/IUUT-PROJECT-DOCUMENTATION.md` §6, §17; `.agent/CODE_STYLE.md`; `.agent/TESTING_CONTRACT.md` |

---

## 1. Prerequisites

| Tool | Version | Notes |
| --- | --- | --- |
| **Windows** | 10 or 11, x64 | IUUT is Windows-only (locked decision Q6). WPF + the game are Windows-only. |
| **.NET SDK** | 8.0.x **or newer** | Projects **target** `net8.0` / `net8.0-windows` (locked decision Q1). `global.json` pins the 8.0 floor with `rollForward: latestMajor`, so a newer installed SDK (e.g. 9.x) builds the net8.0 output fine. If you only have a newer SDK, you're good. If you have *no* .NET SDK, install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). |
| **.NET 8 Desktop Runtime** | 8.0.x | Needed to *run* the WPF app during development. Ships with the .NET 8 SDK; if you build with a newer SDK only, install the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) separately to run IUUT.App. |
| **Git** | 2.30+ | |
| **PowerShell** | 7+ (`pwsh`) recommended | The governance scripts (`scripts/*.ps1`) and the git hooks assume `pwsh`. Windows PowerShell 5.1 works for most but `pwsh` is the tested path. |
| **Bash** | (optional) | The `commit-msg` hook is a bash script; Git for Windows ships Git Bash which satisfies it. |
| **IDE** | Visual Studio 2022 17.8+, JetBrains Rider, or VS Code + C# Dev Kit | Any works. The repo is IDE-agnostic. |

Verify your toolchain:

```powershell
dotnet --version          # 8.0.x or newer
dotnet --list-runtimes    # expect a Microsoft.WindowsDesktop.App 8.0.x entry to RUN the app
git --version
pwsh --version
```

---

## 2. First-time setup

```powershell
# 1. Clone
git clone https://github.com/ImPanick/IUUT.git
cd IUUT

# 2. Install the governance git hooks (REQUIRED — see AGENTS.md §5)
pwsh -File scripts/install-hooks.ps1

# 3. Restore + build to confirm a green baseline
dotnet build IcarusUltimateUtilityTool.sln

# 4. Run the test suite
dotnet test IcarusUltimateUtilityTool.sln
```

If step 2 is skipped, your commits will pass locally but be **rejected at PR time**
when CI runs the equivalent trailer check. Don't skip it.

> **Known-green baseline:** at ground-breaking the scaffold builds with `0 warnings / 0 errors`,
> `1` test passes, and `dotnet format --verify-no-changes` reports no changes. If a fresh
> clone doesn't reproduce that, something in your toolchain differs — fix it before writing code.

---

## 3. Build / test / run / format

| Action | Command |
| --- | --- |
| Build (Debug) | `dotnet build IcarusUltimateUtilityTool.sln` |
| Build (Release) | `dotnet build IcarusUltimateUtilityTool.sln -c Release` |
| Test (all) | `dotnet test IcarusUltimateUtilityTool.sln` |
| Test (one project) | `dotnet test tests/IUUT.Core.Tests/IUUT.Core.Tests.csproj` |
| Test (filter) | `dotnet test --filter "FullyQualifiedName~ProfileParser"` |
| Run the WPF app | `dotnet run --project src/IUUT.App` |
| Run the CLI | `dotnet run --project src/IUUT.Cli -- check` |
| Format (apply) | `dotnet format IcarusUltimateUtilityTool.sln` |
| Format (verify, CI-style) | `dotnet format IcarusUltimateUtilityTool.sln --verify-no-changes` |
| Governance lint | `pwsh -File scripts/governance-lint.ps1 -StagedOnly` |

The four gates CI enforces (mirror them locally before pushing): **build with no
warnings**, **tests pass**, **format verifies clean**, **governance lint clean**.

---

## 4. Project map

| Project | TFM | What goes here |
| --- | --- | --- |
| `src/IUUT.Core` | `net8.0` | All domain logic. Parsers, serializers, mutators, validation, presets, prospect-blob codec, services. **Zero UI deps.** |
| `src/IUUT.Catalog` | `net8.0` | Embedded `D_*` catalog JSON (talents, items, accolades, bestiary, meta-resources). |
| `src/IUUT.App` | `net8.0-windows` | WPF shell. MVVM. **No business logic** — calls into `IUUT.Core`. |
| `src/IUUT.Cli` | `net8.0` | Optional headless CLI (scripting / CI). |
| `tests/IUUT.Core.Tests` | `net8.0` | xUnit + FluentAssertions. |

Folder conventions within each project are documented in that project's `README.md`
and in `.agent/CODE_STYLE.md` §1.

---

## 5. The change lifecycle (short form)

Full ritual: `.agent/AGENT_WORKFLOW.md`. The compressed version:

```
1. Pre-flight   Read AGENTS.md + relevant .agent/ docs. Find the spec section
                that authorizes your task. Check `git log` for other agents' work.

2. Branch       git switch -c agent/<agent>/<task-slug>
                (never commit directly to main — see §7 of this doc)

3. Plan         For anything non-trivial, write the plan (auto-accept if it's
                in-scope and < 200 lines; else get human acceptance).

4. Implement    Small, single-purpose commits. Every commit carries:
                  Agent: <name>/<version>
                  Consulted: AGENTS.md, .agent/CONSTITUTION.md, <spec sections>
                  Co-Authored-By: <Name> <email>

5. Test         Meet .agent/DEFINITION_OF_DONE.md for your change type.
                Run the four gates locally.

6. Hand-off     Open a PR with the template fully filled. Final commit carries
                Handoff-State + Handoff-Notes if you're pausing.
```

---

## 6. Writing a commit that passes the hook

The `commit-msg` hook (`.githooks/commit-msg`) rejects any commit missing the
mandatory trailers. A passing message:

```
feat(core): add ProfileParser with MetaResources round-trip

Implements Profile.json parsing per master doc §8.2. Unknown MetaRow keys
round-trip via System.Text.Json extension data (CONSTITUTION VI).

Agent: claude-code/2.1.149
Consulted: AGENTS.md, .agent/CONSTITUTION.md#VI, docs/IUUT-PROJECT-DOCUMENTATION.md#8.2
Co-Authored-By: Claude <noreply@anthropic.com>
```

Minimum `Consulted:` is `AGENTS.md, .agent/CONSTITUTION.md`. See
`.agent/HANDOFF_PROTOCOL.md` §2 for the full trailer spec.

To author multi-line commit messages on Windows PowerShell, use a single-quoted
here-string:

```powershell
git commit -m @'
feat(core): ...

Agent: ...
Consulted: ...
Co-Authored-By: ...
'@
```

---

## 7. Branching & main

- **`main` is the trunk.** Once branch protection is enabled (see `docs/CICD.md`
  §4), direct pushes to `main` are rejected — all work flows through PRs.
- **Branch names:** `agent/<short-agent>/<task-slug>` (e.g. `agent/claude/profile-parser`).
- **One agent per branch.** Don't work on another agent's branch except to resume
  a documented `wip-blocked` hand-off (`.agent/HANDOFF_PROTOCOL.md` §7).
- **Reserved prefixes:** `release/*`, `governance/*`, `hotfix/*` (see HANDOFF_PROTOCOL §1).

---

## 8. Working with save files during development

Per CONSTITUTION III, VII, and `.agent/SECURITY_PROTOCOL.md`:

- **Never read from your real `%LOCALAPPDATA%\Icarus\Saved\` in tests.** Tests use
  `fixtures/` only.
- **Never commit a real save or a real Steam ID.** Anonymize per SECURITY_PROTOCOL §3
  before anything derived from a live save enters the repo.
- **Manual testing against a real save** is fine *locally* — copy the save out of the
  game directory first, and never let the copy or its contents into a commit.
- **Every save-file mutator** follows `backup → write → re-parse → restore-on-failure`.
  There is no "I'll add the backup later" — the test for it lands in the same PR.

---

## 9. Debugging the WPF app

```powershell
# Run with a debugger attached (from your IDE) or:
dotnet run --project src/IUUT.App -c Debug
```

- Binding errors surface in the IDE Output window. They will become build-time
  errors once the binding-as-errors harness lands (CODE_STYLE §7) — keep bindings clean.
- Design-time data must use anonymized placeholders (`TestUser`, `Char1`) per
  SECURITY_PROTOCOL §4.

---

## 10. Common pitfalls (learned at ground-breaking)

| Symptom | Cause | Fix |
| --- | --- | --- |
| `MSB4025: An XML comment cannot contain '--'` | A `--flag` (e.g. `--self-contained`) inside a `<!-- -->` comment in a `.csproj`. | Don't put `--` inside XML comments. Reference a script/doc instead. |
| `CA1707` on a test method | Analyzer dislikes underscores, but our test-naming convention *requires* them. | Test projects suppress `CA1707` (and `CA1515`) — already wired in `IUUT.Core.Tests.csproj`. |
| `A compatible .NET SDK was not found` | Only a newer SDK installed and `global.json` was too strict. | `global.json` uses `rollForward: latestMajor`; if you still see this, update your SDK or the floor. |
| Commit rejected by hook | Missing `Agent:` / `Consulted:` trailer. | Add the trailers (`.agent/HANDOFF_PROTOCOL.md` §2). Don't `--no-verify`. |
| `dotnet format` fails CI | Local edits didn't match `.editorconfig`. | Run `dotnet format` before pushing. |

---

## 11. Where to look next

| You want to… | Read |
| --- | --- |
| Understand the binding rules | `AGENTS.md` → `.agent/CONSTITUTION.md` |
| Know if your task is in scope | `.agent/SCOPE_GUARDRAILS.md` |
| Follow the change ritual | `.agent/AGENT_WORKFLOW.md` |
| Format a commit / PR | `.agent/HANDOFF_PROTOCOL.md` |
| Know when you're "done" | `.agent/DEFINITION_OF_DONE.md` |
| Write tests | `.agent/TESTING_CONTRACT.md` |
| Understand the pipeline / release | `docs/CICD.md` |
| Understand the save format | `Icarus-Analysis.md` |
| Understand the product | `docs/IUUT-PROJECT-DOCUMENTATION.md` |

---

*Maintained per `.agent/AMENDMENT_PROCESS.md` §4 (Tier-1 normal amendment). Last updated: 2026-05-25.*
