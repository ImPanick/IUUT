# Icarus Ultimate Utility Tool (IUUT)

Unofficial Windows community utility for viewing, repairing, and editing local **Icarus** save files.

> **Status:** Pre-development — documentation-first phase. No executable yet.

## Documentation

All project specification lives in one master document:

**[docs/IUUT-PROJECT-DOCUMENTATION.md](docs/IUUT-PROJECT-DOCUMENTATION.md)**

That file covers:

- Project vision, scope, and locked design decisions
- Complete Icarus save format reference
- Application architecture (.NET 8 + WPF)
- Home presets: Broken Save Recovery, Lazy Max, Custom
- Feature specifications, validation rules, and roadmap

## Quick facts

| | |
| --- | --- |
| **Platform** | Windows x64 only |
| **Stack** | .NET 8, WPF, single-file `IUUT.exe` |
| **Save root** | `%LOCALAPPDATA%\Icarus\Saved\` |
| **Target** | `PlayerData\<SteamID>\` (shown in UI as Steam **display name**) |
| **Online** | Optional — Steam name API + catalog updates; core editing works offline |

## Disclaimer

IUUT is not affiliated with RocketWerkz or the publishers of Icarus. It modifies local files only. Back up your save folder before making changes.

## Related references

- Save format field guide: `%LOCALAPPDATA%\Icarus\Saved\Icarus-Analysis.md`
- Game data catalogs: [Eureka Endeavors](https://icarus.eurekaendeavors.com/catalog/)
