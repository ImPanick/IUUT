# IUUT.Cli

Optional headless CLI.

**Authority:** `docs/IUUT-PROJECT-DOCUMENTATION.md` §6.2.

## Scope

Scripting and CI hooks. Not the primary UX — that's `IUUT.App`. Useful for:

- Power users automating backup / restore.
- CI / fixture-update pipelines.
- Headless test environments.

## Planned commands (per gameplan §5 Phase 0)

| Command | Purpose |
| --- | --- |
| `iuut check` | Health-scan the selected save profile. |
| `iuut backup-all` | Take an `.iuut-backup-*` snapshot of every file under `PlayerData\<SteamID>\`. |
| `iuut lazy-max` | Apply the Lazy Max preset (Characters + Profile + Accolades + Bestiary). |
| `iuut recover` | Run the recovery walker per master doc §12.1. |

Implementation lands as `IUUT.Core` services mature.
