# Changelog

All notable changes to IUUT are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
(see `docs/CICD.md` §5 for the versioning policy).

Governance changes (amendments to `.agent/`) are additionally tracked in their
respective docs' revision-history tables and, once the first amendment lands,
in `docs/GOVERNANCE_CHANGELOG.md`.

## [Unreleased]

### Added

- **Governance contract** for multi-agent development: `AGENTS.md` (universal entry),
  `CLAUDE.md` / `.cursorrules` / `.cursor/rules/agents.mdc` / `.antigravity/rules.md`
  (agent redirectors), and the `.agent/` folder (CONSTITUTION, SCOPE_GUARDRAILS,
  AGENT_WORKFLOW, HANDOFF_PROTOCOL, DEFINITION_OF_DONE, CODE_STYLE, SECURITY_PROTOCOL,
  TESTING_CONTRACT, AMENDMENT_PROCESS, AGENT_REGISTRY).
- **Enforcement plumbing:** `commit-msg` hook, `governance-lint.ps1`, `install-hooks.ps1`,
  PR template, and the Governance Check CI workflow.
- **Solution scaffold** per master doc §17: `IUUT.Core`, `IUUT.Catalog`, `IUUT.App` (WPF),
  `IUUT.Cli`, and `IUUT.Core.Tests`, with `Directory.Build.props`, `.editorconfig`,
  `global.json`, and the solution file.
- **DevOps groundwork:** `docs/DEVELOPMENT.md` and `docs/CICD.md` runbooks, Build & Test
  CI workflow, Dependabot config, `CONTRIBUTING.md`, `SECURITY.md`, this changelog,
  `CODEOWNERS`, and issue templates.
- **Operator-execution guarantees:** `docs/INSTALL.md` operator guide; `release.yml`
  (single-file `IUUT.exe` + portable zip + `SHA256SUMS.txt` + Sigstore build-provenance
  attestation on a `vX.Y.Z` tag); master doc §6.4 (two acquisition paths, no-installer /
  no-admin / no-registry footprint, `%AppData%\IUUT\` default + `IUUT.portable` opt-in,
  clean removal) and §19 release pipeline + user verification.

### Fixed

- Steam name-cache path inconsistency in master doc §7.5.1 (now `%AppData%\IUUT\`).

### Notes

- Pre-development phase. No application behavior is implemented yet; the scaffold builds
  green (0 warnings, 0 errors), one smoke test passes, and `dotnet format` verifies clean.
- The product specification lives in `docs/IUUT-PROJECT-DOCUMENTATION.md`; the save-format
  field guide in `Icarus-Analysis.md`.

---

<!--
  Release entries take this shape once tagging begins (see docs/CICD.md §5):

  ## [0.1.0] - YYYY-MM-DD
  ### Added
  ### Changed
  ### Fixed
  ### Security
-->
