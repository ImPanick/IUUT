# MANUAL_CHECKLIST

Human-only test steps that automated CI cannot perform.

**Authority:** `.agent/TESTING_CONTRACT.md` §1, §9.
**When to run:** Before any release tag. Selectively per PR when reviewer ticks relevant items.

---

## §1. Save-discovery + profile picker

- [ ] Launch `IUUT.exe` on a clean Windows 10 / 11 install (recommended: VM).
- [ ] App auto-resolves `%LOCALAPPDATA%\Icarus\Saved\` without prompting.
- [ ] Profile dropdown lists every `PlayerData\<SteamID64>\` subfolder.
- [ ] Each entry displays the resolved PersonaName (not the raw SteamID64) when local Steam config is available.
- [ ] Each entry shows SteamID64 + character count + last-modified as secondary metadata.
- [ ] Selecting a profile loads its `SaveSession` without exceptions.
- [ ] Save root `Browse...` override works and persists to settings.

## §2. Steam name resolver

- [ ] **Offline (no internet, no Steam Web API key):** PersonaName from local `loginusers.vdf` is shown when available; otherwise SteamID64 is shown with `Connect to resolve Steam name` tooltip.
- [ ] **Online (with API key):** unresolved profiles get a PersonaName from the Steam Web API on next refresh.
- [ ] **Cache:** PersonaName persists in `%AppData%\IUUT\steam-profile-cache.json` and is reused on next launch within TTL.
- [ ] **Refresh button** re-resolves and updates the cache.
- [ ] **Private profile:** API call returns no PersonaName; UI shows SteamID64 + `Private profile` note.

## §3. Game-state banner

- [ ] **Game closed:** banner is hidden (or shows green `Safest`).
- [ ] **Game on Main Menu** (`Icarus-Win64-Shipping.exe` running, on title screen): amber banner `OK — tested`.
- [ ] **Game in any other screen:** yellow banner `Untested — your risk`.
- [ ] **Game in a prospect:** red banner `Strong warning`.
- [ ] In all cases, save operations are **warn-only**, never hard-blocked (CONSTITUTION IX).

## §4. Lazy Max preset

- [ ] Confirmation dialog lists the four files that will be modified and the character count.
- [ ] On apply, `.iuut-backup-<YYYYMMDD-HHMMSS>` files are created next to each modified file.
- [ ] After write, the app re-parses each file and reports success.
- [ ] Launch Icarus, confirm character roster intact, confirm clamp behavior per `Icarus-Analysis.md` §10:
  - Total talents per character ≈ 1067 (account-specific).
  - Rank distribution ≈ 71 % rank 1, ~9/10/10 % at 2/3/4.
  - XP per character ≥ 80M.
  - XP_Debt = 0.
  - No characters lost; no `IsDead` / `IsAbandoned` set unexpectedly.

## §5. Broken Save Recovery

- [ ] Health scan reports OK / restored / template / failed per file.
- [ ] Backup chain restore picks the newest cleanly-parsing candidate.
- [ ] Prospect-specific rule: with ≥2 clean candidates, second-newest is preferred.
- [ ] Template repair runs only when no backup candidates exist; surfaces the partial-recovery banner.
- [ ] For files with no game-managed rotation (MetaInventory.json, AssociatedProspects_*): IUUT's own `.iuut-backup-*` is the fallback, and the algorithm degrades gracefully when none exist.

## §6. Orbital stash UI

- [ ] Stash grid renders MetaInventory items with display names from the embedded catalog.
- [ ] Durability bars match `ItemDynamicData.Durability` against `D_ItemsStatic` max.
- [ ] Repair sets durability to catalog max; re-parse after write succeeds.
- [ ] Replace swaps `ItemStaticData.RowName` and updates the GUID per rules; warns if the GUID is loadout-referenced.
- [ ] Add Item generates a fresh `DatabaseGUID` and writes.
- [ ] Remove warns when the item GUID is referenced by `Loadouts.json`.

## §7. DPI / display

- [ ] At 100 % DPI: layout matches design mockups; no clipped text.
- [ ] At 150 % DPI: layout scales correctly; no clipped text; no blurry rendering.
- [ ] At 200 % DPI: same.
- [ ] Multi-monitor with mixed DPI: `PerMonitorV2` awareness honored (no scaling glitches when moving the window between monitors).

## §8. Privacy / logging

- [ ] Log files in `%LocalAppData%\IUUT\Logs\` contain no real SteamID64.
- [ ] No character names in logs (PersonaName in UI is fine; not in logs).
- [ ] No full file paths with username — paths are relative-from-save-root or use `%USERPROFILE%` placeholders.
- [ ] No outbound network calls observed in network capture other than the optional Steam Web API call (`api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/`).

## §9. Game-load acceptance

- [ ] Launch Icarus after a Lazy Max apply: title screen loads.
- [ ] Character selection screen lists all characters with names intact.
- [ ] Entering a prospect succeeds without immediate corruption errors.
- [ ] No anti-cheat / VAC warnings (Icarus is single-player so this should be inherently safe; flag if anything surfaces).

## §10. Release smoke

- [ ] Single-file publish (`dotnet publish ... -p:PublishSingleFile=true`) produces `IUUT.exe` 15–25 MB.
- [ ] Launches on a Windows 10 / 11 machine without .NET 8 preinstalled (self-contained).
- [ ] No console window appears for the WPF app.
- [ ] First launch shows the legal disclaimer (master doc §4.1).

## §11. Acquisition integrity (release artifacts)

- [ ] `release.yml` produced `IUUT.exe`, `IUUT-portable.zip`, and `SHA256SUMS.txt` on the GitHub Release.
- [ ] `(Get-FileHash IUUT.exe -Algorithm SHA256).Hash` matches the `IUUT.exe` line in `SHA256SUMS.txt`.
- [ ] `gh attestation verify IUUT.exe --repo ImPanick/IUUT` passes (provenance ties the exe to the tagged build).
- [ ] **Acquisition Path B parity:** a from-source `dotnet publish` build runs and behaves identically to the downloaded exe.

## §12. No-install footprint & portable mode

- [ ] Running `IUUT.exe` requires **no installer**, **no admin / UAC prompt**, and creates **no** Start-Menu entry or shortcut.
- [ ] Default mode: the only folder created is `%AppData%\IUUT\` (cache, encrypted key, logs, settings). Confirm with a before/after diff of `%AppData%` and the registry.
- [ ] No writes to the Windows registry, Program Files, or any machine-wide location.
- [ ] Native single-file extraction lands under the IUUT state folder (`%AppData%\IUUT\runtime\`), **not** loose in `%TEMP%`.
- [ ] **Portable mode:** with an `IUUT.portable` marker beside the exe, all state goes to `.\IUUT-Data\` and **nothing** is written to `%AppData%`. Verify on a USB stick.
- [ ] **Clean removal:** deleting `IUUT.exe` + the one state folder leaves no IUUT trace; Icarus saves and their `.iuut-backup-*` files are untouched.

---

## Sign-off

Reviewer fills in:

```
Date:        ______________________
Build:       ______________________
Reviewer:    ______________________
Notes:       ______________________
```

The PR review may tick the subset relevant to the change under review; full checklist is required only before a tagged release.
