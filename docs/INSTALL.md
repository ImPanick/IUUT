# Get & Run IUUT — Operator Guide

How to obtain, verify, and run **Icarus Ultimate Utility Tool (IUUT)**. No
programming required for the download path.

> **The promise:** IUUT is a single `.exe`. You don't install anything, it doesn't
> need administrator rights, it makes no system-wide changes, and removing it is
> deleting one file and one folder. See the [footprint](#5-what-iuut-leaves-on-your-pc)
> section for the exact list of what it touches.

---

## 1. Pick how you get it

| You want… | Use |
| --- | --- |
| The easy way — just download and run | **Path A: pre-built download** (§2) |
| To compile it yourself and trust nothing but your own build | **Path B: build from source** (§3) |

Both give you the **identical** self-contained `IUUT.exe`.

---

## 2. Path A — download the pre-built `IUUT.exe` (recommended)

1. Go to the repository's **Releases** page:
   <https://github.com/ImPanick/IUUT/releases>
2. From the latest release, download:
   - **`IUUT.exe`** (or **`IUUT-portable.zip`** if you want portable mode — see §6), and
   - **`SHA256SUMS.txt`**
3. **Verify it** (strongly recommended — see §4). It takes ten seconds.
4. **Double-click `IUUT.exe`.** That's it. No setup, no install, no admin prompt.

> Windows SmartScreen may show "Windows protected your PC" the first time, because
> IUUT is not yet Authenticode-signed (a paid certificate is a future upgrade). Click
> **More info → Run anyway**. Verifying the file first (§4) is exactly how you confirm
> it's safe despite that warning.

---

## 3. Path B — build your own `IUUT.exe` from source

Requires the **.NET 8 SDK** (or newer — see `docs/DEVELOPMENT.md` §1). Then:

```powershell
git clone https://github.com/ImPanick/IUUT.git
cd IUUT

dotnet publish src/IUUT.App/IUUT.App.csproj `
  -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Your `IUUT.exe` is written to
`src/IUUT.App/bin/Release/net8.0-windows/win-x64/publish/IUUT.exe`.
Copy it anywhere and double-click. This is a binary **you** produced from source —
no need to trust our release at all.

---

## 4. Verify the download ("verified signed hashes")

Two independent checks. Do at least the first.

### 4a. Checksum (no tools needed beyond PowerShell)

```powershell
(Get-FileHash .\IUUT.exe -Algorithm SHA256).Hash
```

Compare the printed hash against the `IUUT.exe` line in `SHA256SUMS.txt`. They must
match exactly (case-insensitive). A mismatch means the file is corrupted or tampered —
do not run it.

### 4b. Build provenance (recommended; needs the free GitHub CLI)

```powershell
gh attestation verify .\IUUT.exe --repo ImPanick/IUUT
```

This cryptographically proves the `.exe` was built by IUUT's public CI from a specific
tagged commit (Sigstore attestation) — not swapped out by someone else. A green result
is the strongest assurance short of building it yourself.

---

## 5. What IUUT leaves on your PC

IUUT is deliberately tidy. The complete footprint:

| Item | Where | What it is |
| --- | --- | --- |
| The program | wherever you put `IUUT.exe` | The single executable. |
| App state | `%AppData%\IUUT\` | Steam-name cache, your (encrypted) Steam Web API key if you set one, logs, and settings. The **only** folder IUUT creates. |
| Save backups | inside your `…\Icarus\Saved\PlayerData\<SteamID>\` | Timestamped `.iuut-backup-…` copies made **before** every edit, next to the files they protect. Part of your save folder, not scattered. |

IUUT does **not**: run an installer, require admin, write to the Windows registry,
install into Program Files, create Start-Menu entries or shortcuts, run a background
service, auto-start with Windows, or phone home.

---

## 6. Portable mode (true fire-and-forget)

Want **nothing** in `%AppData%`? Run portable:

- Use the `IUUT-portable.zip` release (it includes the marker), **or**
- Create an empty file named **`IUUT.portable`** next to `IUUT.exe`.

In portable mode, all app state goes in a **`IUUT-Data\`** folder beside the `.exe`.
Put the exe + that folder on a USB stick and it travels with you, leaving the host PC
completely untouched.

---

## 7. First run

1. **Read & accept the one-time disclaimer** (IUUT is unofficial; back up your saves;
   you're responsible for your edits).
2. **Save folder auto-links.** IUUT looks for `%LOCALAPPDATA%\Icarus\Saved\`
   automatically. If found, you go straight to the profile picker.
3. **If it can't find your saves** (game installed somewhere unusual), IUUT asks you to
   **Browse…** to your `Saved\` (or `PlayerData\`) folder once. It remembers your choice.
4. **Pick your profile** by Steam display name and start with **Broken Save Recovery**,
   **Lazy Max**, or **Custom**.

---

## 8. Remove IUUT completely

There's nothing to uninstall:

1. Delete `IUUT.exe`.
2. Delete `%AppData%\IUUT\` (or, in portable mode, the `IUUT-Data\` folder next to the exe).

Your Icarus saves and their backups are left untouched. (If you also want to discard
the backups IUUT made, delete the `*.iuut-backup-*` files in your `PlayerData\<SteamID>\`
folder — but you may want to keep them.)

---

## 9. Safety reminders

- **Back up your save folder** before big edits (IUUT also auto-backs-up each file).
- **Close Icarus, or stay on the Main Menu**, while editing (see the in-app banner).
- If you use **Steam Cloud**, verify sync direction after editing so an older cloud
  copy doesn't overwrite your edits.

---

*Operator runbook. The binding guarantees behind it live in
`docs/IUUT-PROJECT-DOCUMENTATION.md` §6.4 and §19. Maintained per
`.agent/AMENDMENT_PROCESS.md` §4. Last updated: 2026-05-25.*
