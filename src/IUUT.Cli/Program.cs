// IUUT.Cli — headless entry point for scripting and CI (master doc §6.2).
// Hand-rolled verb dispatch: the CLI's needs do not clear the dependency gate
// (SCOPE_GUARDRAILS §2.6) for an argument-parsing package.

using System.Globalization;
using IUUT.Core.Abstractions;
using IUUT.Core.Catalog;
using IUUT.Core.DataPak;
using IUUT.Core.Editing;
using IUUT.Core.Io;
using IUUT.Core.Services;

if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
{
    PrintUsage();
    return 0;
}

string[] rootOnly = ["--root"];
string[] profileAndRoot = ["--profile", "--root"];
string[] applyFlag = ["--apply"];
string[] pakValue = ["--pak"];
string[] forceFlag = ["--force"];
string[] none = [];

try
{
    return args[0] switch
    {
        "check" => Check(ParseOptions(args, rootOnly, none)),
        "backup-all" => BackupAll(ParseOptions(args, rootOnly, none)),
        "lazy-max" => await LazyMaxAsync(ParseOptions(args, profileAndRoot, applyFlag)).ConfigureAwait(false),
        "catalog-refresh" => CatalogRefresh(ParseOptions(args, pakValue, forceFlag)),
        "prospect-report" => ProspectReport(ParseOptions(args, profileAndRoot, none)),
        "quest-reset" => await QuestResetAsync(ParseOptions(args, ["--prospect", "--profile", "--root"], applyFlag)).ConfigureAwait(false),
        "homestead-move" => await HomesteadMoveAsync(
            ParseOptions(args, ["--prospect", "--profile", "--root", "--build", "--by", "--radius"], ["--apply", "--snap"])).ConfigureAwait(false),
        "rescue-character" => await RescueCharacterAsync(
            ParseOptions(args, ["--prospect", "--profile", "--root", "--character", "--to"], ["--apply", "--snap", "--revive"])).ConfigureAwait(false),
        "return-to-stash" => await ReturnToStashAsync(
            ParseOptions(args, ["--prospect", "--profile", "--root"], applyFlag)).ConfigureAwait(false),
        "recover" => Recover(),
        _ => UnknownCommand(args[0]),
    };
}
#pragma warning disable CA1031 // Top-level boundary: report any failure as exit code 1 instead of a crash dump.
catch (Exception ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 1;
}
#pragma warning restore CA1031

static void PrintUsage()
{
    Console.WriteLine("iuut — Icarus Ultimate Utility Tool (headless CLI)");
    Console.WriteLine();
    Console.WriteLine("usage: iuut <command> [options]");
    Console.WriteLine();
    Console.WriteLine("  check            [--root <path>]");
    Console.WriteLine("                   Read-only health scan of every profile. Exit 0 = all healthy,");
    Console.WriteLine("                   2 = issues found.");
    Console.WriteLine("  backup-all       [--root <path>]");
    Console.WriteLine("                   Timestamped IUUT backups of every save file in every profile.");
    Console.WriteLine("  lazy-max         --profile <steamid-or-path> [--apply] [--root <path>]");
    Console.WriteLine("                   Preview the Lazy Max pipeline (default), or apply it with");
    Console.WriteLine("                   --apply (backups are created first, writes are atomic).");
    Console.WriteLine("  catalog-refresh  [--pak <path>] [--force]");
    Console.WriteLine("                   Refresh the catalog cache from the installed game's data.pak");
    Console.WriteLine("                   (offline; sanity-gated; a rejected refresh changes nothing).");
    Console.WriteLine("  prospect-report  [--profile <steamid-or-path>] [--root <path>]");
    Console.WriteLine("                   Read-only report over each prospect world save: faction");
    Console.WriteLine("                   mission + quest-step state, trapped-item totals, and what");
    Console.WriteLine("                   you have built. Defaults to the root's first profile.");
    Console.WriteLine("  quest-reset      --prospect <name> [--profile <steamid-or-path>] [--apply]");
    Console.WriteLine("                   Reset a prospect's mission progress so it can be replayed.");
    Console.WriteLine("                   Preview by default; --apply writes (backup first, atomic,");
    Console.WriteLine("                   size-preserving — items, mounts, and bases are untouched).");
    Console.WriteLine("  homestead-move   --prospect <name> [--build <n>] [--by <x,y,z>] [--radius <m>]");
    Console.WriteLine("                   [--snap] [--profile <steamid-or-path>] [--apply]");
    Console.WriteLine("                   List what you have built, grouped into separate builds; with");
    Console.WriteLine("                   --build and --by, relocate one build by that many metres.");
    Console.WriteLine("                   Reports the estimated ground height at the destination and");
    Console.WriteLine("                   how sure it is; --snap picks the z offset that lands the");
    Console.WriteLine("                   build on it. Preview by default; --apply writes (backup");
    Console.WriteLine("                   first, atomic, size-preserving).");
    Console.WriteLine();
    Console.WriteLine("  STUCK IN A PROSPECT? (host-side — everyone must be out of the prospect first)");
    Console.WriteLine("  return-to-stash  --prospect <name> [--profile <steamid-or-path>] [--apply]");
    Console.WriteLine("                   Pull items trapped in a prospect back into your orbital stash —");
    Console.WriteLine("                   for when the host is gone or the world will not resume. The stash");
    Console.WriteLine("                   is written first, so an interrupted return can only duplicate.");
    Console.WriteLine("  rescue-character --prospect <name> [--character <n>] [--to <x,y,z>] [--snap]");
    Console.WriteLine("                   [--revive] [--profile <steamid-or-path>] [--apply]");
    Console.WriteLine("                   List the characters recorded in a prospect and move one somewhere");
    Console.WriteLine("                   reachable — for a zone that reset behind you, or a boss that");
    Console.WriteLine("                   glitched and stranded a body. Carried gear travels with them.");
    Console.WriteLine();
    Console.WriteLine("  recover          Guided save recovery lives in the IUUT app — it needs the UI.");
    Console.WriteLine();
    Console.WriteLine("  --root defaults to %LOCALAPPDATA%\\Icarus\\Saved.");
}

// Strict per-command parsing: an unknown option or a missing value is an error, never a silent
// no-op (a typo like --froce or a forgotten --root value must not quietly change behavior).
static Dictionary<string, string?> ParseOptions(string[] args, string[] valueOptions, string[] flags)
{
    var options = new Dictionary<string, string?>(StringComparer.Ordinal);
    for (var i = 1; i < args.Length; i++)
    {
        var name = args[i];
        if (!name.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"unexpected argument '{name}' (options start with --)");
        }

        if (flags.Contains(name))
        {
            options[name] = null;
            continue;
        }

        if (!valueOptions.Contains(name))
        {
            throw new ArgumentException($"unknown option '{name}' for this command — run `iuut help`");
        }

        if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"option '{name}' requires a value");
        }

        options[name] = args[++i];
    }

    return options;
}

static string ResolveRoot(Dictionary<string, string?> options)
{
    var root = options.GetValueOrDefault("--root") ?? SaveDiscoveryService.ResolveDefaultSaveRoot();
    if (!new SaveDiscoveryService().SaveRootContainsPlayerData(root))
    {
        throw new ArgumentException($"no PlayerData folder under '{root}' (pass --root <path-to-Saved>)");
    }

    return root;
}

static int Check(Dictionary<string, string?> options)
{
    var root = ResolveRoot(options);
    var profiles = new SaveDiscoveryService().DiscoverProfiles(root);
    var scanner = new HealthScanService();
    var totalIssues = 0;

    // A healthy save can still be holding your gear hostage. `check` is the first thing anyone
    // runs when something has gone wrong, so it has to name the way out — a recovery feature you
    // cannot find at the moment you need it may as well not exist.
    var stranded = new List<string>();

    foreach (var profile in profiles)
    {
        var report = scanner.ScanProfile(profile.FolderPath);
        totalIssues += report.IssueCount;
        Console.WriteLine($"{profile.SteamId64}: {report.OkCount} ok, {report.IssueCount} issues");
        foreach (var issue in report.Issues)
        {
            Console.WriteLine($"  {issue.Status}: {issue.RelativePath}{(issue.Detail is null ? "" : " — " + issue.Detail)}");
        }

        stranded.AddRange(ScanForStrandedGear(profile.FolderPath));
    }

    Console.WriteLine(totalIssues == 0
        ? $"All healthy ({profiles.Count} profiles)."
        : $"{totalIssues} issue(s) across {profiles.Count} profiles.");

    if (stranded.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Items and characters are sitting inside prospects:");
        foreach (var line in stranded)
        {
            Console.WriteLine($"  {line}");
        }

        Console.WriteLine();
        Console.WriteLine("  That is normal for a prospect you can still get back into. If you CANNOT —");
        Console.WriteLine("  the host is gone, the world will not resume, a zone reset behind you, or a boss");
        Console.WriteLine("  glitched and stranded your body — you can get it back without the game:");
        Console.WriteLine("    iuut return-to-stash  --prospect <name>   pull the items into your orbital stash");
        Console.WriteLine("    iuut rescue-character --prospect <name>   move a stranded character somewhere reachable");
        Console.WriteLine("  Both preview by default. In the app: RESCUE → \"Stuck in a prospect?\".");
    }

    return totalIssues == 0 ? 0 : 2;
}

// Per-prospect summary of what is stranded, for `check`. Resilient by design: a prospect that
// will not parse is exactly the case the user needs help with, so it must not abort the scan.
static IEnumerable<string> ScanForStrandedGear(string profileFolder)
{
    var prospects = Path.Combine(profileFolder, "Prospects");
    if (!Directory.Exists(prospects))
    {
        yield break;
    }

    var returns = new ProspectReturnService(new StashEditService(new SystemGuidProvider()));
    var characters = new IUUT.Core.Prospects.World.ProspectCharacterReader();

    foreach (var file in Directory.EnumerateFiles(prospects, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
    {
        if (file.Contains(BackupManager.BackupInfix, StringComparison.Ordinal))
        {
            continue;
        }

        string? line = null;
#pragma warning disable CA1031 // An unreadable prospect is the Recovery screen's problem, not this summary's.
        try
        {
            var model = IUUT.Core.Parsers.ProspectFileParser.Parse(File.ReadAllText(file));
            var items = returns.Preview(model);
            var people = characters.ReadBlob(model.ProspectBlob);
            var dead = people.Count(p => !p.IsAlive);
            var carrying = people.Sum(p => p.CarriedSlots);

            if (items.Count > 0 || carrying > 0 || dead > 0)
            {
                var parts = new List<string>();
                if (items.Count > 0)
                {
                    parts.Add($"{items.Sum(i => i.TotalQuantity)} item(s) in {items.Count} kind(s)");
                }

                if (people.Count > 0)
                {
                    parts.Add($"{people.Count} character(s){(dead > 0 ? $", {dead} DEAD" : "")}"
                            + $"{(carrying > 0 ? $" carrying {carrying} slot(s)" : "")}");
                }

                line = $"{Path.GetFileNameWithoutExtension(file)}: {string.Join("; ", parts)}";
            }
        }
        catch (Exception)
        {
            line = null;
        }
#pragma warning restore CA1031

        if (line is not null)
        {
            yield return line;
        }
    }
}

static int BackupAll(Dictionary<string, string?> options)
{
    var root = ResolveRoot(options);
    var profiles = new SaveDiscoveryService().DiscoverProfiles(root);
    var backups = new BackupManager(new SystemClock());
    var count = 0;

    var failed = 0;
    foreach (var profile in profiles)
    {
        foreach (var file in Directory.EnumerateFiles(profile.FolderPath, "*", SearchOption.AllDirectories))
        {
            if (file.Contains(BackupManager.BackupInfix, StringComparison.Ordinal))
            {
                continue; // never back up a backup
            }

#pragma warning disable CA1031 // Per-file resilience: one locked/vanished file must not abort the whole backup run.
            try
            {
                backups.CreateBackup(file);
                count++;
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"  FAILED: {file} — {ex.Message}");
            }
#pragma warning restore CA1031
        }

        Console.WriteLine($"{profile.SteamId64}: backed up");
    }

    Console.WriteLine(failed == 0
        ? $"{count} file(s) backed up across {profiles.Count} profiles."
        : $"{count} file(s) backed up across {profiles.Count} profiles; {failed} FAILED (see above).");
    return failed == 0 ? 0 : 1;
}

static async Task<int> LazyMaxAsync(Dictionary<string, string?> options)
{
    var target = options.GetValueOrDefault("--profile")
        ?? throw new ArgumentException("lazy-max requires --profile <steamid-or-path>");
    string folder;
    if (Directory.Exists(target))
    {
        folder = target;
    }
    else if (target.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal)
          || target.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
    {
        // A path was given — do not resolve the save root, whose error would mislead.
        throw new ArgumentException($"profile folder not found: '{target}'");
    }
    else
    {
        folder = Path.Combine(ResolveRoot(options), SaveDiscoveryService.PlayerDataFolder, target);
        if (!Directory.Exists(folder))
        {
            throw new ArgumentException($"profile folder not found: '{folder}'");
        }
    }

    var paths = AppPaths.Resolve();
    var catalogs = GameCatalogs.LoadWithCache(paths.CatalogCacheDirectory);
    var lazyMax = new LazyMaxService(catalogs, new SystemClock());
    var writer = new SafeSaveWriter(new BackupManager(new SystemClock()), new SystemGuidProvider());
    var apply = new LazyMaxApplyService(lazyMax, writer);

    var plan = await apply.PreviewAsync(folder).ConfigureAwait(false);
    if (plan.Result is { } r)
    {
        Console.WriteLine($"Preview: {r.CharactersMaxed} characters ({r.TalentsPerCharacter} talents each), "
            + $"{r.MetaResourcesMaxed} currencies, +{r.WorkshopUnlocksAdded} workshop unlocks, "
            + $"+{r.AccoladesAdded} accolades, +{r.BestiaryGroupsAdded} bestiary groups, "
            + $"{r.MissionFlagsSet} mission flags — {plan.Files.Count} file(s) would be written.");
    }

    if (!plan.CanApply)
    {
        Console.Error.WriteLine("Validation failed — nothing can be applied:");
        foreach (var issue in plan.Validation.Issues)
        {
            Console.Error.WriteLine($"  {issue.Severity}: {issue.Message}");
        }

        return 1;
    }

    if (!options.ContainsKey("--apply"))
    {
        Console.WriteLine("Preview only. Re-run with --apply to write (backups are created first).");
        return 0;
    }

    var report = await apply.ApplyAsync(plan).ConfigureAwait(false);
    foreach (var file in report.FileResults)
    {
        Console.WriteLine($"  {(file.Ok ? "wrote" : "FAILED")}: {file.FilePath}");
    }

    Console.WriteLine(report.Applied ? "Applied." : $"Apply failed: {report.Message}");
    return report.Applied ? 0 : 1;
}

static int CatalogRefresh(Dictionary<string, string?> options)
{
    if (options.GetValueOrDefault("--pak") is { } pakOverride && !File.Exists(pakOverride))
    {
        // An explicit --pak must be honored or rejected — never silently swapped for the Steam pak.
        Console.Error.WriteLine($"--pak path not found: '{pakOverride}'");
        return 1;
    }

    var pak = CatalogRefreshRunner.LocatePak(options.GetValueOrDefault("--pak"));
    if (pak is null)
    {
        Console.Error.WriteLine("data.pak not found (pass --pak <path> or install the game via Steam).");
        return 1;
    }

    var runner = new CatalogRefreshRunner(AppPaths.Resolve());
    // NOT Progress<T>: with no SynchronizationContext its callbacks queue to the thread pool and
    // can print out of order or be dropped at process exit.
    var progress = new ConsoleProgress();
    var result = runner.RefreshIfStale(pak, force: options.ContainsKey("--force"), progress);
    if (result is null)
    {
        return 0; // already fresh (reported via progress)
    }

    foreach (var line in result.Report)
    {
        Console.WriteLine($"  {line}");
    }

    Console.WriteLine(result.Ok ? "Catalog cache refreshed." : "Refresh REJECTED — cache untouched.");
    return result.Ok ? 0 : 1;
}

static int ProspectReport(Dictionary<string, string?> options)
{
    var folder = ResolveProfileFolder(options);
    var prospectsDir = Path.Combine(folder, "Prospects");
    if (!Directory.Exists(prospectsDir))
    {
        Console.WriteLine("No prospect world saves in this profile.");
        return 0;
    }

    var quests = new IUUT.Core.Prospects.World.ProspectQuestReader();
    var trappedPreview = new ProspectReturnService(new StashEditService(new SystemGuidProvider()));

    foreach (var file in Directory.EnumerateFiles(prospectsDir, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
    {
        Console.WriteLine($"\n== {Path.GetFileNameWithoutExtension(file)} ==");
        IUUT.Core.Models.ProspectFileModel model;
        try
        {
            model = IUUT.Core.Parsers.ProspectFileParser.Parse(File.ReadAllText(file));
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or FormatException or IOException)
        {
            Console.WriteLine($"  unreadable ({ex.GetType().Name}) — the Recovery screen can help.");
            continue;
        }

        try
        {
            var state = quests.ReadBlob(model.ProspectBlob);
            if (state.HasMission)
            {
                Console.WriteLine($"  mission: {state.MissionName} — {(state.MissionComplete ? "COMPLETE" : "in progress")}");
                Console.WriteLine($"  steps:   {state.Steps.Count(s => s.IsComplete)}/{state.Steps.Count} complete");
                foreach (var step in state.Steps)
                {
                    Console.WriteLine($"    {(step.IsComplete ? "[done]" : "[    ]")} {step.QuestName}");
                }
            }
            else
            {
                Console.WriteLine("  mission: none (open world)");
            }

            var trapped = trappedPreview.Preview(model);
            Console.WriteLine(trapped.Count == 0
                ? "  items:   nothing trapped"
                : $"  items:   {trapped.Count} kind(s), {trapped.Sum(t => t.TotalQuantity)} total — recover via Return to Stash");

            var homestead = new IUUT.Core.Prospects.World.ProspectHomesteadReader().ReadBlob(model.ProspectBlob);
            if (homestead.Structures.Count == 0)
            {
                Console.WriteLine($"  base:    nothing built ({homestead.TotalActors} world actors)");
            }
            else
            {
                Console.WriteLine($"  base:    {homestead.Structures.Count} structure(s) of {homestead.TotalActors} actors");
                foreach (var (rowName, count) in homestead.ByKind.Take(8))
                {
                    Console.WriteLine($"    {count,4}x {rowName}");
                }

                if (homestead.ByKind.Count > 8)
                {
                    Console.WriteLine($"    … {homestead.ByKind.Count - 8} more kind(s)");
                }

                if (homestead.Footprint is { } fp)
                {
                    Console.WriteLine($"    where:  centre ({fp.X:N0}, {fp.Y:N0}) m, elevation {fp.Z:N0} m, " +
                                      $"spread {fp.SpanMetres:N0} m across {homestead.Placements.Count} placed piece(s)");
                }

                Console.WriteLine($"    links:  {homestead.FoundationLinked} anchored to a foundation, " +
                                  $"{homestead.WhitelistLinked} with a tame whitelist");
                Console.WriteLine($"    ids:    {homestead.DistinctActorGuids} actor ids in use (max {homestead.MaxActorGuid}), " +
                                  $"{homestead.TileNames.Count} terrain tile(s) referenced");
            }
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException)
        {
            Console.WriteLine($"  blob unreadable ({ex.Message})");
        }
    }

    return 0;
}

// --profile <steamid-or-path>, defaulting to the root's first profile when omitted.
static string ResolveProfileFolder(Dictionary<string, string?> options)
{
    if (options.GetValueOrDefault("--profile") is { } target)
    {
        if (Directory.Exists(target))
        {
            return target;
        }

        if (target.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal)
         || target.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException($"profile folder not found: '{target}'");
        }

        var byId = Path.Combine(ResolveRoot(options), SaveDiscoveryService.PlayerDataFolder, target);
        if (!Directory.Exists(byId))
        {
            throw new ArgumentException($"profile folder not found: '{byId}'");
        }

        return byId;
    }

    var profiles = new SaveDiscoveryService().DiscoverProfiles(ResolveRoot(options));
    if (profiles.Count == 0)
    {
        throw new ArgumentException("no save profiles found (pass --profile or --root)");
    }

    if (profiles.Count > 1)
    {
        Console.WriteLine($"({profiles.Count} profiles — using {profiles[0].SteamId64}; pass --profile to pick another)");
    }

    return profiles[0].FolderPath;
}

static async Task<int> QuestResetAsync(Dictionary<string, string?> options)
{
    var prospectName = options.GetValueOrDefault("--prospect")
        ?? throw new ArgumentException("quest-reset requires --prospect <name> (see prospect-report for names)");
    var folder = ResolveProfileFolder(options);
    var path = Path.Combine(folder, "Prospects", prospectName + ".json");
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"prospect not found: '{path}'");
        return 1;
    }

    var model = IUUT.Core.Parsers.ProspectFileParser.Parse(await File.ReadAllTextAsync(path).ConfigureAwait(false));
    var reader = new IUUT.Core.Prospects.World.ProspectQuestReader();
    var before = reader.ReadBlob(model.ProspectBlob);
    if (!before.HasMission && before.Steps.Count == 0)
    {
        Console.WriteLine("This prospect has no mission or quest state to reset.");
        return 0;
    }

    Console.WriteLine($"mission: {before.MissionName} — {(before.MissionComplete ? "COMPLETE" : "in progress")}");
    Console.WriteLine($"steps:   {before.Steps.Count(s => s.IsComplete)}/{before.Steps.Count} complete");

    var result = IUUT.Core.Prospects.World.ProspectQuestEditor.ResetMission(model);
    Console.WriteLine($"reset would clear: {result.StepsReset} step(s), {result.VariablesCleared} variable(s)"
        + (result.ManagerCleared ? ", the mission-complete flag" : ""));

    if (!result.Changed)
    {
        Console.WriteLine("Nothing to reset — the mission is already at its initial state.");
        return 0;
    }

    if (!options.ContainsKey("--apply"))
    {
        Console.WriteLine("Preview only. Re-run with --apply to write (a backup is taken first).");
        return 0;
    }

    var clock = new SystemClock();
    var files = new CustomFileService(
        new SafeSaveWriter(new BackupManager(clock), new SystemGuidProvider()),
        new BackupManager(clock));
    var save = await files
        .SaveJsonTextAsync(path, IUUT.Core.Serializers.ProspectFileSerializer.Serialize(model))
        .ConfigureAwait(false);
    if (!save.Ok)
    {
        Console.Error.WriteLine($"Write failed; the original prospect is unchanged. {save.Error?.Message}");
        return 1;
    }

    var after = reader.ReadBlob(
        IUUT.Core.Parsers.ProspectFileParser.Parse(await File.ReadAllTextAsync(path).ConfigureAwait(false)).ProspectBlob);
    Console.WriteLine($"APPLIED: steps now {after.Steps.Count(s => s.IsComplete)}/{after.Steps.Count} complete — backup at {save.BackupPath}");
    return 0;
}

// Lists a prospect's builds, and — given --build and --by — relocates one of them.
// Preview-first like every other write verb: --apply is the only thing that touches the save.
static async Task<int> HomesteadMoveAsync(Dictionary<string, string?> options)
{
    var prospectName = options.GetValueOrDefault("--prospect")
        ?? throw new ArgumentException("homestead-move requires --prospect <name> (see prospect-report for names)");
    var folder = ResolveProfileFolder(options);
    var path = Path.Combine(folder, "Prospects", prospectName + ".json");
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"prospect not found: '{path}'");
        return 1;
    }

    var radius = options.GetValueOrDefault("--radius") is { } radiusText
        ? ParseMetres(radiusText, "--radius")
        : 60;
    if (radius <= 0)
    {
        throw new ArgumentException("--radius must be greater than 0 metres");
    }

    var model = IUUT.Core.Parsers.ProspectFileParser.Parse(await File.ReadAllTextAsync(path).ConfigureAwait(false));
    var reader = new IUUT.Core.Prospects.World.ProspectHomesteadReader();
    var clusters = reader.ReadBlob(model.ProspectBlob).Clusters(radius);
    if (clusters.Count == 0)
    {
        Console.WriteLine("Nothing built here that can be placed on the map.");
        return 0;
    }

    Console.WriteLine($"{clusters.Count} build(s) in '{prospectName}' (pieces within {radius:N0} m count as one build):");
    foreach (var build in clusters)
    {
        Console.WriteLine($"  [{build.Index}] {build.Count,4} piece(s) at ({build.CentreX:N0}, {build.CentreY:N0}) m, "
            + $"elevation {build.CentreZ:N0} m, spread {build.SpanMetres:N0} m");
        Console.WriteLine($"       {string.Join(", ", build.TopKinds)}");
    }

    if (options.GetValueOrDefault("--by") is not { } byText)
    {
        Console.WriteLine("\nPass --build <n> --by <x,y,z> (metres) to relocate one of these.");
        return 0;
    }

    var offsets = byText.Split(',');
    if (offsets.Length != 3)
    {
        throw new ArgumentException("--by takes three metre offsets: --by <x,y,z> (e.g. --by 250,-125,0)");
    }

    var dx = ParseMetres(offsets[0], "--by x");
    var dy = ParseMetres(offsets[1], "--by y");
    var dz = ParseMetres(offsets[2], "--by z");

    var index = options.GetValueOrDefault("--build") is { } buildText
        ? (int.TryParse(buildText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new ArgumentException($"--build takes a build number from the list above, not '{buildText}'"))
        : 0;
    if (index < 0 || index >= clusters.Count)
    {
        throw new ArgumentException($"--build {index} does not exist — pick 0..{clusters.Count - 1} from the list above");
    }

    var target = clusters[index];

    // Estimate the ground at both ends. Comparing the two preserves the build's relationship to
    // the terrain — sitting 2 m proud of the ground here means sitting 2 m proud there — without
    // needing to know how the build is footed.
    var terrain = IUUT.Core.Prospects.World.TerrainHeightField.FromProspect(model);
    var here = terrain.EstimateAt(target.CentreX, target.CentreY);
    var there = terrain.EstimateAt(target.CentreX + dx, target.CentreY + dy);
    double? suggestedDz = here is not null && there is not null ? there.HeightMetres - here.HeightMetres : null;

    if (options.ContainsKey("--snap"))
    {
        if (suggestedDz is null)
        {
            Console.Error.WriteLine("--snap needs a ground estimate at both ends, and this prospect has too few "
                                  + "world actors to give one. Re-run with an explicit z offset.");
            return 1;
        }

        dz = suggestedDz.Value;
    }

    var result = IUUT.Core.Prospects.World.ProspectHomesteadEditor.MoveCluster(model, target, dx, dy, dz);
    if (!result.Changed)
    {
        Console.WriteLine("\nNothing to move — a zero offset leaves the build where it is.");
        return 0;
    }

    Console.WriteLine($"\nMove build [{index}] — {result.StructuresMoved} piece(s) — by ({dx:N0}, {dy:N0}, {dz:N0}) m:");
    Console.WriteLine($"  from ({target.CentreX:N0}, {target.CentreY:N0}, {target.CentreZ:N0}) m");
    Console.WriteLine($"  to   ({target.CentreX + dx:N0}, {target.CentreY + dy:N0}, {target.CentreZ + dz:N0}) m");
    Console.WriteLine("  Structures keep their shape, contents, and anchoring; nothing else in the save changes.");

    // Ground report. IUUT infers this from the world's own actors — say so, and say how sure it is.
    Console.WriteLine($"\n  Ground height (estimated from {terrain.SampleCount:N0} world features, not surveyed):");
    if (there is null)
    {
        Console.WriteLine("    Not enough world features in this prospect to estimate. Treat the drop as unknown.");
    }
    else
    {
        Console.WriteLine($"    at the destination: {there.HeightMetres:N0} m — {there.Confidence} confidence");
        Console.WriteLine($"      {there.Explanation}");
        if (suggestedDz is not null)
        {
            var landing = dz - suggestedDz.Value;
            Console.WriteLine(Math.Abs(landing) < 1
                ? "    The build should land about level with the ground."
                : $"    The build would land about {Math.Abs(landing):N0} m {(landing > 0 ? "ABOVE" : "BELOW")} the ground"
                  + $" — {(landing > 0 ? "floating" : "buried")}.");

            if (!options.ContainsKey("--snap") && Math.Abs(landing) >= 1)
            {
                Console.WriteLine($"    Add --snap to use a z offset of {suggestedDz.Value:N0} m instead and sit it on the ground.");
            }
        }

        if (there.Confidence == IUUT.Core.Prospects.World.TerrainHeightConfidence.Low)
        {
            Console.WriteLine("    LOW CONFIDENCE — this is a guess. Move somewhere flatter, or expect to re-level by hand.");
        }
    }

    if (!options.ContainsKey("--apply"))
    {
        Console.WriteLine("Preview only. Re-run with --apply to write (a backup is taken first).");
        return 0;
    }

    var clock = new SystemClock();
    var files = new CustomFileService(
        new SafeSaveWriter(new BackupManager(clock), new SystemGuidProvider()),
        new BackupManager(clock));
    var save = await files
        .SaveJsonTextAsync(path, IUUT.Core.Serializers.ProspectFileSerializer.Serialize(model))
        .ConfigureAwait(false);
    if (!save.Ok)
    {
        Console.Error.WriteLine($"Write failed; the original prospect is unchanged. {save.Error?.Message}");
        return 1;
    }

    var after = reader
        .ReadBlob(IUUT.Core.Parsers.ProspectFileParser.Parse(await File.ReadAllTextAsync(path).ConfigureAwait(false)).ProspectBlob)
        .Clusters(radius);
    var moved = after.FirstOrDefault(c => c.Count == target.Count);
    Console.WriteLine(moved is null
        ? $"APPLIED: {result.StructuresMoved} piece(s) moved — backup at {save.BackupPath}"
        : $"APPLIED: {result.StructuresMoved} piece(s) now at ({moved.CentreX:N0}, {moved.CentreY:N0}) m — backup at {save.BackupPath}");
    return 0;
}

static double ParseMetres(string text, string option) =>
    double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
        ? value
        : throw new ArgumentException($"{option} takes a distance in metres, not '{text.Trim()}'");

// Pulls the items trapped in a prospect back into the orbital stash. This shipped in the app in
// v2.1.0 but had no CLI verb, which is half of why nobody could find it when they needed it.
static async Task<int> ReturnToStashAsync(Dictionary<string, string?> options)
{
    var prospectName = options.GetValueOrDefault("--prospect")
        ?? throw new ArgumentException("return-to-stash requires --prospect <name> (see prospect-report for names)");
    var folder = ResolveProfileFolder(options);
    var path = Path.Combine(folder, "Prospects", prospectName + ".json");
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"prospect not found: '{path}'");
        return 1;
    }

    var clock = new SystemClock();
    var service = new ProspectReturnFileService(
        new CustomFileService(
            new SafeSaveWriter(new BackupManager(clock), new SystemGuidProvider()),
            new BackupManager(clock)),
        new ProspectReturnService(new StashEditService(new SystemGuidProvider())));

    var trapped = await service.PreviewAsync(path).ConfigureAwait(false);
    if (trapped.Count == 0)
    {
        Console.WriteLine("Nothing is trapped in this prospect.");
        return 0;
    }

    Console.WriteLine($"{trapped.Sum(t => t.TotalQuantity)} item(s) across {trapped.Count} kind(s) in '{prospectName}':");
    foreach (var item in trapped.OrderByDescending(t => t.TotalQuantity).Take(20))
    {
        Console.WriteLine($"  {item.TotalQuantity,6} x {item.RowName}  ({item.SlotCount} slot(s))");
    }

    if (trapped.Count > 20)
    {
        Console.WriteLine($"  … {trapped.Count - 20} more kind(s)");
    }

    if (!options.ContainsKey("--apply"))
    {
        Console.WriteLine("\nPreview only. Re-run with --apply to move these into your orbital stash");
        Console.WriteLine("(the stash is written FIRST, so an interrupted return can only duplicate, never lose).");
        return 0;
    }

    var result = await service.ReturnAsync(path, folder).ConfigureAwait(false);
    if (!result.Ok)
    {
        Console.Error.WriteLine($"Return failed; nothing was lost. {result.Error}");
        return 1;
    }

    Console.WriteLine($"APPLIED: {result.Moved?.TotalQuantity ?? 0} item(s) returned to the orbital stash "
        + $"as {result.Moved?.StashStacksAdded ?? 0} stack(s).");
    Console.WriteLine("Everyone must be OUT of the prospect when you do this, or the running session will overwrite it.");
    return 0;
}

// Frees a character the game has stranded — a zone that reset behind you, a boss that glitched and
// pinned bodies somewhere unreachable. The state lives only in the host's prospect save.
static async Task<int> RescueCharacterAsync(Dictionary<string, string?> options)
{
    var prospectName = options.GetValueOrDefault("--prospect")
        ?? throw new ArgumentException("rescue-character requires --prospect <name> (see prospect-report for names)");
    var folder = ResolveProfileFolder(options);
    var path = Path.Combine(folder, "Prospects", prospectName + ".json");
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"prospect not found: '{path}'");
        return 1;
    }

    var model = IUUT.Core.Parsers.ProspectFileParser.Parse(await File.ReadAllTextAsync(path).ConfigureAwait(false));
    var reader = new IUUT.Core.Prospects.World.ProspectCharacterReader();
    var characters = reader.ReadBlob(model.ProspectBlob);
    if (characters.Count == 0)
    {
        Console.WriteLine("No characters are recorded in this prospect.");
        return 0;
    }

    Console.WriteLine($"{characters.Count} character(s) in '{prospectName}':");
    for (var i = 0; i < characters.Count; i++)
    {
        var c = characters[i];
        var where = c.Location is null ? "position unknown" : FormattableString.Invariant(
            $"at ({c.Location.Metres.X:N0}, {c.Location.Metres.Y:N0}, {c.Location.Metres.Z:N0}) m");
        Console.WriteLine($"  [{i}] player {c.MaskedPlayerId} · character slot {c.CharacterSlot} · "
            + $"{(c.IsAlive ? "alive" : "DEAD")} · {c.Health} hp · {where}");
        Console.WriteLine($"       carrying {c.CarriedSlots} item slot(s); {c.RespawnCount} respawn(s) used");
    }

    if (options.GetValueOrDefault("--to") is not { } toText)
    {
        Console.WriteLine("\nPass --character <n> --to <x,y,z> (metres) to move one somewhere reachable.");
        Console.WriteLine("Add --snap to drop them on the estimated ground, and --revive if they are dead.");
        return 0;
    }

    var parts = toText.Split(',');
    if (parts.Length != 3)
    {
        throw new ArgumentException("--to takes a world position in metres: --to <x,y,z> (e.g. --to -890,815,-235)");
    }

    var tx = ParseMetres(parts[0], "--to x");
    var ty = ParseMetres(parts[1], "--to y");
    var tz = ParseMetres(parts[2], "--to z");

    var index = options.GetValueOrDefault("--character") is { } text
        ? (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new ArgumentException($"--character takes a number from the list above, not '{text}'"))
        : 0;
    if (index < 0 || index >= characters.Count)
    {
        throw new ArgumentException($"--character {index} does not exist — pick 0..{characters.Count - 1} from the list above");
    }

    var target = characters[index];
    var terrain = IUUT.Core.Prospects.World.TerrainHeightField.FromProspect(model);
    var ground = terrain.EstimateAt(tx, ty);

    if (options.ContainsKey("--snap"))
    {
        if (ground is null)
        {
            Console.Error.WriteLine("--snap needs a ground estimate and this prospect has too few world features. "
                                  + "Give an explicit z instead.");
            return 1;
        }

        // Stand them just above the ground rather than exactly on it, so they settle rather than clip.
        tz = ground.HeightMetres + 1;
    }

    var revive = options.ContainsKey("--revive");
    Console.WriteLine($"\nMove player {target.MaskedPlayerId} (character slot {target.CharacterSlot})"
        + $"{(revive ? " and revive them" : "")}:");
    if (target.Location is not null)
    {
        Console.WriteLine(FormattableString.Invariant(
            $"  from ({target.Location.Metres.X:N0}, {target.Location.Metres.Y:N0}, {target.Location.Metres.Z:N0}) m"));
    }

    Console.WriteLine(FormattableString.Invariant($"  to   ({tx:N0}, {ty:N0}, {tz:N0}) m"));
    Console.WriteLine($"  Their {target.CarriedSlots} carried item slot(s) travel with them — the gear is on the body.");

    if (ground is not null)
    {
        Console.WriteLine($"  Ground there is about {ground.HeightMetres:N0} m ({ground.Confidence} confidence) — "
            + $"{ground.Explanation}.");
        if (!options.ContainsKey("--snap") && Math.Abs(tz - ground.HeightMetres) > 5)
        {
            Console.WriteLine("  That z is well off the ground; --snap would place them on it.");
        }
    }

    if (!target.IsAlive && !revive)
    {
        Console.WriteLine("  This character is DEAD — moving the body alone may not be enough. Add --revive.");
    }

    var result = IUUT.Core.Prospects.World.ProspectCharacterEditor.Rescue(model, target, tx, ty, tz, revive);
    if (!result.Changed)
    {
        Console.Error.WriteLine("Nothing was changed — the character record could not be matched.");
        return 1;
    }

    if (!options.ContainsKey("--apply"))
    {
        Console.WriteLine("Preview only. Re-run with --apply to write (a backup is taken first).");
        return 0;
    }

    var clock = new SystemClock();
    var files = new CustomFileService(
        new SafeSaveWriter(new BackupManager(clock), new SystemGuidProvider()),
        new BackupManager(clock));
    var save = await files
        .SaveJsonTextAsync(path, IUUT.Core.Serializers.ProspectFileSerializer.Serialize(model))
        .ConfigureAwait(false);
    if (!save.Ok)
    {
        Console.Error.WriteLine($"Write failed; the original prospect is unchanged. {save.Error?.Message}");
        return 1;
    }

    Console.WriteLine($"APPLIED{(result.Revived ? " (moved and revived)" : " (moved)")} — backup at {save.BackupPath}");
    Console.WriteLine("Everyone must be OUT of the prospect when you do this, or the running session will overwrite it.");
    return 0;
}

static int Recover()
{
    Console.WriteLine("Guided recovery (scan → plan → repair with report) lives in the IUUT app:");
    Console.WriteLine("it needs per-file decisions that don't fit a non-interactive CLI.");
    Console.WriteLine("Run `iuut check` here to find problems, then use the app's Recovery screen.");
    return 0;
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"unknown command '{command}' — run `iuut help`.");
    return 1;
}

/// <summary>Synchronous console progress (miner phase lines print in order, before the summary).</summary>
internal sealed class ConsoleProgress : IProgress<string>
{
    /// <inheritdoc />
    public void Report(string value) => Console.WriteLine(value);
}
