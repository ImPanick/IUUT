# SECURITY_PROTOCOL — Data sensitivity, anonymization, secrets

> **Tier 2 — Binding for any task touching save files, fixtures, logs, or commits.**
> This document defines what may not be committed, what must be scrubbed, and how to handle secrets.

| | |
| --- | --- |
| **Document** | Security Protocol |
| **Version** | 1.0.0 |
| **Authority** | CONSTITUTION VII (Anonymization) |
| **Amendment rule** | `.agent/AMENDMENT_PROCESS.md` |

---

## §1. Threat model

IUUT is a local-only tool that operates on save files in `%LOCALAPPDATA%\Icarus\Saved\PlayerData\<SteamID64>\`. The data on disk includes:

- SteamID64 (17-digit account identifier, weakly identifying).
- PersonaName / character names (potentially identifying).
- Cosmetic preferences, talent choices, prospect history (low sensitivity but personal).
- Steam Web API key (if user provides one — see master doc §7.5.1).

The repository is public on GitHub (or will be). The threats we mitigate:

1. **Accidental commit of real user data** — Steam IDs, character names, full paths.
2. **Commit of a Steam Web API key** — these are tied to a real user's Steam account.
3. **Logs that ship with builds containing PII** — would expose users to telemetry-equivalent leakage even though IUUT itself has no telemetry.
4. **Fixtures that look anonymized but aren't** — partial scrubbing is worse than none.

---

## §2. Hard prohibitions (never committed)

| Item | Pattern | Where it appears | Mitigation |
| --- | --- | --- | --- |
| Real SteamID64 | `7656119\d{10}` | save fixtures, doc examples, log samples, commit messages | Replace with `00000000000000000` |
| Real character names | (depends) | fixtures, screenshots, doc examples | Replace with `Char1`, `Char2`, `Char3` |
| Real PersonaName | (depends) | UI mockups, doc examples, screenshots | Replace with `TestUser` |
| Steam Web API key | 32-hex character string | code, config templates, env files | Never; require user-provided at runtime |
| Real file paths | `C:\Users\<real-username>\...` | docs, scripts, code | Use `%LOCALAPPDATA%`, `%USERPROFILE%`, or `<UserName>` placeholders |
| Email addresses | RFC-822-shaped | (any) | Substitute `noreply@example.com` |
| Discord IDs, Steam friend IDs | numeric | docs, examples | Scrub |
| OAuth tokens, refresh tokens | Base64-shaped, long | (any) | Never commit; if found, rotate immediately |

`scripts/governance-lint.ps1` enforces the first two via regex. The rest are reviewer-enforced (and lint can be extended on demand).

---

## §3. Anonymization standard

When deriving a fixture or example from a live save:

1. **SteamID64:** every occurrence → `00000000000000000`. Includes folder names, JSON values, log samples, file content.
2. **Character names:** map deterministically — first character encountered → `Char1`, second → `Char2`, etc. Document the mapping in the fixture's adjacent README if downstream tests need to know which is which.
3. **PersonaName:** → `TestUser`. Same mapping rule if multiple PersonaNames in one fixture.
4. **Prospect IDs / GUIDs:** keep the format (32-hex), regenerate the value — `00000000000000000000000000000000` is fine, or a fresh `Guid.NewGuid().ToString("N").ToUpperInvariant()`.
5. **Timestamps:** retain shape (`YYYY.MM.DD-HH.MM.SS` or Unix epoch), but use a stable reference date (e.g., `2020.01.01-00.00.00` or epoch 0) unless the test depends on chronological order — in which case use sequential offsets.
6. **Paths / hostnames in `AssociatedProspects`:** scrub Steam P2P IDs (16-digit numeric) and any dedicated-server hostnames/IPs.
7. **Cosmetic block:** values are integer indices; retain as-is (not sensitive).

After scrubbing, **re-run the parser** against the scrubbed fixture and confirm it still round-trips. Broken fixtures are worse than no fixtures.

---

## §4. Logging policy

### What may appear in logs

- File operation summary: `"Loaded profile from save root"` (no path).
- Counts: `"Wrote 1067 talents to character slot 1"` (slot index OK; character name not).
- Outcomes: `"Backup created", "Re-parse succeeded", "Restored from backup after failure"`.
- Validation issues: rule name + path within JSON (`"Profile.UserID mismatch at Profile.json"`).
- Error type + sanitized message.

### What may NOT appear in logs

- SteamID64 (even partially redacted — first/last 4 chars is still identifying).
- Character names.
- PersonaName.
- Full file paths (relative paths from save root are OK).
- Any value derived from the user's Steam credentials.
- Stack traces in production builds **if** they include local paths with usernames. Use a path-redactor middleware.

### Implementation

- `IUUT.Core.Logging.SafeFormatter` provides scrubbing helpers: `Sanitize.SteamId(string)` returns `"steamid-<hash6>"`, `Sanitize.Path(string)` returns relative-from-save-root.
- `ILogger` calls in `IUUT.Core` must use sanitized values. Code review enforces.

---

## §5. Secrets handling

### Steam Web API key

- **User-provided only.** No bundled key in shipped builds (per master doc §7.5.1 the option exists, but v1 ships user-provided only; if the bundled-key path is ever added, an amendment PR justifies the rate-limit and key-rotation plan).
- **Storage:** Windows DPAPI (`System.Security.Cryptography.ProtectedData.Protect` with `DataProtectionScope.CurrentUser`). The encrypted blob lives at `%AppData%\IUUT\steam-api-key.bin`.
- **Never in `appsettings.json`, `.env`, environment variables**, or any file that could be committed.
- **Never log the key** — not even partially, not even hashed.
- **On detection of accidental commit:** the user rotates the key on `steamcommunity.com/dev/apikey` immediately. We do not "wait to see if anyone fetched it."

### Other secrets

The project intentionally has no other secrets (no auth, no signing keys in source, no API tokens). If a future feature introduces one, an amendment PR justifies the introduction and the storage mechanism.

---

## §6. Pre-commit scrub checklist

Before any commit that touches `fixtures/`, `docs/`, or any sample data:

- [ ] Search the diff for `7656119` — if found, scrub.
- [ ] Search the diff for the current user's Windows username — if found, replace with `<UserName>` or `%USERPROFILE%`.
- [ ] Search for known character names (from your own play sessions if you're a contributor) — replace.
- [ ] Search for 32-hex strings — if any are real GUIDs from your save, regenerate.
- [ ] Run `pwsh -File scripts/governance-lint.ps1` against the staged diff.

`scripts/governance-lint.ps1` automates checks 1, 2, and 5. Checks 3 and 4 require human awareness because the script can't know your character names.

---

## §7. Incident response

If a real Steam ID, character name, or other PII is committed to the public repo:

1. **Stop further commits.** Do not push more on top of the violation.
2. **Determine if the commit reached `origin/main`** (pushed) or only local.
3. **If local-only:** amend or reset; re-commit with scrubbed content; push only after verification.
4. **If pushed:** the commit is in history forever (force-push only after explicit human approval; users may have already cloned). Open a `security` issue, scrub the file, commit the scrubbed version, and:
   - Notify the human operator immediately.
   - If a Steam API key was leaked, rotate it.
   - If a real Steam ID was leaked, the affected user is informed if known.
5. **Document the incident** in a `SECURITY_INCIDENTS.md` (to be created on first incident, then maintained).

---

## §8. Third-party data (catalogs)

`IUUT.Catalog` embeds JSON derived from [Eureka Endeavors](https://icarus.eurekaendeavors.com/catalog/). The data itself is public game-table content (RowNames, display names, max ranks); no PII.

When adding new catalog data:

- **Cite the source URL** in the catalog file's header comment.
- **Cite the fetch date** so version drift is auditable.
- **Do not commit the scraper output verbatim** — pass it through a normalizer that drops anything not in the schema we use.
- **License: confirm** Eureka Endeavors data is OK to redistribute under our MIT. (Currently presumed OK as it mirrors game assets; revisit if challenged.)

---

## Revision history

| Version | Date | Change |
| --- | --- | --- |
| 1.0.0 | 2026-05-25 | Adopted alongside CONSTITUTION v1.0.0. |
