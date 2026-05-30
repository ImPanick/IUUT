# CICD — Pipelines, branch protection, versioning, releases

> How code moves from a developer's branch to a tagged `IUUT.exe`. The day-to-day
> dev runbook is [`docs/DEVELOPMENT.md`](DEVELOPMENT.md); the behavioral contract is
> `AGENTS.md` + `.agent/`.

| | |
| --- | --- |
| **Audience** | Anyone operating the repo's automation; release managers |
| **Authority** | `docs/IUUT-PROJECT-DOCUMENTATION.md` §16, §18, §19; `.agent/DEFINITION_OF_DONE.md`; `.agent/CODE_STYLE.md` §14 |

---

## 1. Pipeline overview

Two GitHub Actions workflows gate every PR into `main`:

| Workflow | File | Runs | Purpose |
| --- | --- | --- | --- |
| **Governance Check** | `.github/workflows/governance-check.yml` | PR open/sync/edit | Validates PR-body contract sections, commit trailers, and runs `governance-lint.ps1` (PII + style). |
| **Build & Test** | `.github/workflows/build.yml` | PR + push to main | Restore → build (warnings-as-errors) → test → `dotnet format --verify-no-changes`. |

Both must be green to merge. Neither builds artifacts — release packaging is a
separate, human-triggered process (§5).

```
   ┌──────────────┐     PR      ┌─────────────────────┐
   │  agent/<...>  │ ─────────▶ │  Governance Check    │ ─┐
   │  feature      │            │  (body + trailers +  │  │  both green
   │  branch       │            │   PII lint)          │  ├────────────▶  merge to main
   └──────────────┘            └─────────────────────┘  │
                               ┌─────────────────────┐  │
                               │  Build & Test        │ ─┘
                               │  (build/test/format) │
                               └─────────────────────┘
```

---

## 2. Governance Check workflow

Jobs:

1. **pr-body-check** — fails if the PR body is missing any required section from
   `.github/PULL_REQUEST_TEMPLATE.md`, or if the Consultation section omits
   `AGENTS.md` / `.agent/CONSTITUTION.md`, or if the Agent section names no
   registered agent.
2. **commit-trailer-check** — every commit in the PR must carry `Agent:` and
   (`Consulted:` or `Governance-Override:`) trailers; `Consulted:` must include the
   two mandatory docs.
3. **governance-lint** — runs `scripts/governance-lint.ps1` over the PR diff
   (SteamID / hardcoded-path PII scan, BOM-encoder check, template presence).

Authority: `.agent/CONSTITUTION.md` VIII, X; `.agent/HANDOFF_PROTOCOL.md` §2, §3.

---

## 3. Build & Test workflow

Single `build-test` job on `windows-latest` (WPF requires Windows):

1. `actions/setup-dotnet@v4` installs the **.NET 8 SDK** (`8.0.x`) so the `net8.0`
   targeting pack is guaranteed present. `global.json` (`rollForward: latestMajor`)
   permits a newer runner SDK to perform the build; the output is still `net8.0`.
2. `dotnet restore`
3. `dotnet build -c Release` — `TreatWarningsAsErrors=true` (via `Directory.Build.props`)
   makes any analyzer warning a build failure.
4. `dotnet test -c Release` with coverage collection.
5. `dotnet format --verify-no-changes` — style gate per CODE_STYLE §14.
6. Coverage uploaded as a build artifact.

> **Reproducibility note.** The *target framework* is the pinned, reproducible thing
> (locked decision Q1). The *build SDK* may roll forward to a newer major; this is an
> accepted trade-off chosen so contributors aren't forced to install an exact SDK
> patch. If strict SDK reproducibility is ever required, tighten `global.json`
> `rollForward` and pin `setup-dotnet` to an exact version — that's a Tier-1
> amendment, not a casual change.

---

## 4. Branch protection (enable at ground-breaking)

The governance docs describe the **steady state**: `main` is protected, all work is
PR-gated. To make that real, the repo owner enables branch protection on `main`
in **GitHub → Settings → Branches → Add rule**:

- [x] Require a pull request before merging
  - [x] Require approvals: **1** (a human for Tier-0 changes; see §6)
  - [x] Dismiss stale approvals on new commits
- [x] Require status checks to pass before merging
  - Required checks: **`Build, test, format`** (from Build & Test) and the
    **Governance Check** jobs (`pr-body-check`, `commit-trailer-check`, `governance-lint`)
  - [x] Require branches to be up to date before merging
- [x] Require conversation resolution before merging
- [x] Require signed commits *(optional; enable if contributors can sign)*
- [x] Do not allow bypassing the above settings *(applies rules to admins too)*
- [x] Restrict who can push to matching branches *(no direct pushes)*

### Bootstrap-phase exception (why the first commits went straight to `main`)

Branch protection and the CI gates cannot meaningfully protect a repository whose
CI does not yet exist. The first commits — the governance contract, the solution
scaffold, and this DevOps groundwork (the very workflows that *do* the gating) —
are therefore made directly on `main` as a **bootstrap exception**. The bootstrap
phase **ends the moment branch protection is enabled**, which should be done
immediately after this groundwork lands. From that point, everyone — humans and
agents alike — uses the branch + PR flow with no exceptions. This exception is a
one-time, self-terminating bootstrap reality, not a standing license to push to main.

---

## 5. Release process

Releases are **human-triggered** (agents propose readiness, humans tag — see
`.agent/HANDOFF_PROTOCOL.md` §9). SemVer per master doc §19.

### Versioning policy (SemVer)

| Bump | When |
| --- | --- |
| **Major** (`X.0.0`) | Breaking change to save-file handling, public `IUUT.Core` API removal, or a locked-decision change. |
| **Minor** (`0.X.0`) | New feature (a new preset, a new editable category) — backward compatible. |
| **Patch** (`0.0.X`) | Bug fix, catalog refresh, doc-only release. |

Milestones map to master doc §16: `0.1` MVP (Lazy Max + backup + Main-Menu verified),
`0.2` Recovery, `0.3`–`0.4` Custom, `1.0` full Custom + public release.

### Release runbook

1. Confirm `main` is green (both workflows).
2. Open a `release-readiness` issue; an agent may assemble the checklist, a human signs off.
3. Run the relevant items in `tests/MANUAL_CHECKLIST.md` (UI + game-load acceptance).
4. Tag: `git tag vX.Y.Z && git push origin vX.Y.Z` (human only).
5. `pwsh -File scripts/publish-release.ps1` produces the self-contained single-file
   `IUUT.exe` (~15–25 MB) plus a portable zip. *(Script is a stub until first code release.)*
6. Create the GitHub Release; attach `IUUT.exe` + portable zip; paste the generated
   release notes (commit log since the previous tag, grouped by `<type>`).
7. Update `CHANGELOG.md` (move `Unreleased` items under the new version heading).

### Code signing (future)

Optional, reduces SmartScreen friction (master doc §19). When a cert is available,
`publish-release.ps1` signs `IUUT.exe`. Not required for early releases.

---

## 6. Approval gates

| Change class | Gate | Authority |
| --- | --- | --- |
| Routine code / docs | 1 reviewer (human or different-model agent) + both CI workflows green | DEFINITION_OF_DONE §1 |
| `.agent/` Tier-1/2 amendment | 1 reviewer + `governance-amendment` label | AMENDMENT_PROCESS §4 |
| `.agent/CONSTITUTION.md` or locked-decision (master §6.1) change | **Two-signoff: 1 human + 1 different-model agent** | AMENDMENT_PROCESS §3 |
| New dependency | Human approval + security/license review | SCOPE_GUARDRAILS §2.6, SECURITY_PROTOCOL §8 |
| New AI agent onboarding | Tier-0 amendment + onboarding-test PR | AGENT_REGISTRY §4 |

`CODEOWNERS` (`.github/CODEOWNERS`) routes review of `.agent/`, locked-decision
docs, and the enforcement plumbing to the owner so these gates are not bypassed.

---

## 7. Dependency management

`.github/dependabot.yml` proposes weekly version bumps for NuGet packages and
GitHub Actions. Dependabot **only bumps already-approved dependencies** — adding a
brand-new package is still an out-of-scope action requiring escalation
(SCOPE_GUARDRAILS §2.6). Review each bump against SECURITY_PROTOCOL §8 before merge.

---

## 8. What CI does **not** do (yet)

- **No deployment.** IUUT is a local desktop tool; there is no server, no environment
  to deploy to. "Release" = a GitHub Release artifact, nothing more.
- **No auto-publish.** Packaging is human-triggered (§5).
- **No telemetry / analytics pipeline.** Categorically prohibited (CONSTITUTION V).
- **No secret storage in CI.** The build needs no secrets. If a future signing step
  needs a cert, it goes in GitHub Actions encrypted secrets, never in the repo.

---

*Maintained per `.agent/AMENDMENT_PROCESS.md` §4 (Tier-1 normal amendment). Last updated: 2026-05-25.*
