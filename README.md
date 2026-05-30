# Icarus Ultimate Utility Tool (IUUT)

Unofficial Windows community utility for viewing, repairing, and editing local
**Icarus** (RocketWerkz) save files.

> **Status:** Pre-development → **breaking ground.** The governance contract,
> solution scaffold, and CI/CD groundwork are in place; application code is next.
> The scaffold builds green (0 warnings / 0 errors), one smoke test passes, and
> `dotnet format` verifies clean.

---

## What IUUT does

Two equally-valid jobs, from one Windows desktop app:

1. **Save recovery** — corrupted JSON, crash mid-write, interrupted updates, Steam
   Cloud conflicts, characters stuck in prospects, partial data loss.
2. **Convenience editing** — max talents, currencies, accolades, bestiary, orbital
   stash management, and fine-grained tweaks without manual JSON surgery.

Three home-screen workflows: **Broken Save Recovery**, **Lazy Max**, and **Custom**.

## Get IUUT

Two ways to get the **same** single-file `IUUT.exe` — full guide in
**[docs/INSTALL.md](docs/INSTALL.md)**:

- **Download (recommended):** grab `IUUT.exe` + `SHA256SUMS.txt` from
  [Releases](https://github.com/ImPanick/IUUT/releases), verify the hash and the
  build-provenance attestation (`gh attestation verify`), then double-click.
- **Build it yourself:** clone and run the `dotnet publish` command in
  [docs/INSTALL.md §3](docs/INSTALL.md).

**No install, no admin, no registry.** IUUT is one `.exe`; its only footprint is a
single `%AppData%\IUUT\` folder (or a portable `IUUT-Data\` beside the exe). Removal =
delete the exe + that folder. The save folder auto-links on launch; if it can't be
found you point IUUT at it once.

## Quick facts

| | |
| --- | --- |
| **Platform** | Windows 10/11 x64 only |
| **Stack** | .NET 8, WPF, self-contained single-file `IUUT.exe` |
| **Save root** | `%LOCALAPPDATA%\Icarus\Saved\` |
| **Target** | `PlayerData\<SteamID>\` (shown in UI as Steam **display name**) |
| **Online** | Optional — Steam name API + catalog updates; core editing works offline |
| **Telemetry** | **None.** No analytics, no cloud upload, no accounts (CONSTITUTION V). |

---

## Repository map

```
IUUT/
├── AGENTS.md                     ← start here if you're contributing (human or agent)
├── CLAUDE.md / .cursorrules / …  ← per-agent redirectors to AGENTS.md
├── .agent/                       ← the binding governance contract (CONSTITUTION etc.)
├── .github/                      ← CI workflows, PR/issue templates, CODEOWNERS, dependabot
├── .githooks/                    ← commit-msg governance hook
├── docs/
│   ├── IUUT-PROJECT-DOCUMENTATION.md   ← master spec (what to build)
│   ├── DEVELOPMENT.md                  ← local dev runbook (how to build/test/run)
│   ├── CICD.md                         ← pipelines, branch protection, releases
│   └── *.plan.md                       ← POC + legacy plans
├── Icarus-Analysis.md            ← save-format field guide (technical source of truth)
├── src/
│   ├── IUUT.Core/                ← domain logic (zero UI deps)
│   ├── IUUT.Catalog/             ← embedded D_* catalog data
│   ├── IUUT.App/                 ← WPF shell
│   └── IUUT.Cli/                 ← optional headless CLI
├── tests/IUUT.Core.Tests/        ← xUnit + FluentAssertions
├── catalogs/  fixtures/  scripts/
└── IcarusUltimateUtilityTool.sln
```

## Build & run

```powershell
git clone https://github.com/ImPanick/IUUT.git
cd IUUT
pwsh -File scripts/install-hooks.ps1     # REQUIRED governance hook
dotnet build IcarusUltimateUtilityTool.sln
dotnet test  IcarusUltimateUtilityTool.sln
dotnet run --project src/IUUT.App        # (placeholder shell for now)
```

Full prerequisites and workflow: **[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)**.

---

## Contributing — this is a governed multi-agent repo

IUUT is developed by **multiple AI coding agents** (Claude Code, OpenAI Codex,
Cursor, Google Antigravity) **and humans**, all bound by an enforceable contract.
Before touching anything:

1. **[AGENTS.md](AGENTS.md)** — the universal contract.
2. **[.agent/CONSTITUTION.md](.agent/CONSTITUTION.md)** — immutable principles.
3. **[CONTRIBUTING.md](CONTRIBUTING.md)** — the contribution loop.
4. **[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)** — set up and build.

Every commit cites the docs it consulted and declares its agent identity; the
`commit-msg` hook and CI enforce this. See **[docs/CICD.md](docs/CICD.md)** for the
pipeline and **[SECURITY.md](SECURITY.md)** for the disclosure policy.

## Documentation index

| Doc | Purpose |
| --- | --- |
| [docs/IUUT-PROJECT-DOCUMENTATION.md](docs/IUUT-PROJECT-DOCUMENTATION.md) | Master spec — vision, scope, save model, architecture, features, roadmap |
| [Icarus-Analysis.md](Icarus-Analysis.md) | Save-format field guide (empirically verified) |
| [docs/INSTALL.md](docs/INSTALL.md) | **Operator guide** — get, verify, run, footprint, portable, removal |
| [docs/IMPLEMENTATION-PLAN.md](docs/IMPLEMENTATION-PLAN.md) | **Build roadmap** — work packages, critical path, where to start |
| [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) | Local developer runbook |
| [docs/CICD.md](docs/CICD.md) | CI/CD, branch protection, versioning, releases |
| [AGENTS.md](AGENTS.md) + [.agent/](.agent/) | Multi-agent governance contract |
| [CONTRIBUTING.md](CONTRIBUTING.md) · [SECURITY.md](SECURITY.md) · [CHANGELOG.md](CHANGELOG.md) | Community + process |

---

## Disclaimer

IUUT is not affiliated with RocketWerkz or the publishers of Icarus. It modifies
local files only. **Back up your save folder before making changes.** Multiplayer
hosts should coordinate with their group before editing shared prospect files.

## License

[MIT](LICENSE).

## Related references

- Game data catalogs: [Eureka Endeavors](https://icarus.eurekaendeavors.com/catalog/)
