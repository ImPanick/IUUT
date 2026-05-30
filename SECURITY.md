# Security Policy

IUUT is a **local-only** desktop tool. It has no servers, no accounts, no telemetry,
and (other than an optional user-keyed Steam Web API name lookup) makes no network
calls. The security surface is therefore small but not zero — this policy covers it.

> Contributor-facing data-handling rules live in
> [`.agent/SECURITY_PROTOCOL.md`](.agent/SECURITY_PROTOCOL.md). This file is the
> **public, outward-facing** disclosure policy.

## What we consider a security/privacy issue

- **Committed PII** — a real SteamID64, character name, PersonaName, or personal
  file path that landed in the repo or a release artifact.
- **A leaked secret** — e.g. a Steam Web API key committed to the repo.
- **A save-corruption vector** — a defect by which IUUT can corrupt or destroy a
  user's save without the backup/restore safety net engaging.
- **An un-enumerated network call** — any outbound connection beyond the documented
  optional Steam Web API lookup (master doc §7.5.1). IUUT must never phone home.
- **Code execution / supply-chain** — a dependency or build step that could execute
  untrusted code on a user's machine.

## Reporting

**Please do not open a public issue for a sensitive vulnerability.**

- Preferred: open a **GitHub private security advisory** via the repository's
  **Security → Advisories → Report a vulnerability** tab.
- Alternative: contact the maintainer (**ImPanick**) through the GitHub profile.

Include: what you found, how to reproduce it, the affected version/commit, and the
impact (e.g. "leaks a real Steam ID", "corrupts Characters.json on this input").

We aim to acknowledge within a few days. Because IUUT is a community project, response
times are best-effort.

## What happens next

1. We confirm and assess severity.
2. For a **leaked secret**, we ask the owner to rotate it immediately (e.g. regenerate
   the Steam Web API key at steamcommunity.com/dev/apikey) — we do not wait to see if it
   was scraped.
3. For **committed PII**, we scrub it and follow the incident steps in
   `.agent/SECURITY_PROTOCOL.md` §7. Note that git history is durable; a force-push to
   rewrite history happens only with explicit owner approval.
4. For a **save-corruption vector**, we treat it as the highest-priority class of bug
   (a player's hundreds of hours are at stake) and may use the emergency-fix path in
   `.agent/AMENDMENT_PROCESS.md` §7.
5. We credit reporters who want credit, once a fix is released.

## Supported versions

IUUT is pre-1.0. Until a `1.0.0` release, only the latest `main` is supported. After
1.0, the latest released minor line receives security fixes.

## Scope notes

- IUUT edits **local files the user already owns**. It is not a multiplayer cheat,
  memory injector, or process hook (master doc §2.3). Reports premised on those uses
  are out of scope.
- The game itself (Icarus / RocketWerkz) is out of scope — report game vulnerabilities
  to RocketWerkz, not here.
