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

    foreach (var profile in profiles)
    {
        var report = scanner.ScanProfile(profile.FolderPath);
        totalIssues += report.IssueCount;
        Console.WriteLine($"{profile.SteamId64}: {report.OkCount} ok, {report.IssueCount} issues");
        foreach (var issue in report.Issues)
        {
            Console.WriteLine($"  {issue.Status}: {issue.RelativePath}{(issue.Detail is null ? "" : " — " + issue.Detail)}");
        }
    }

    Console.WriteLine(totalIssues == 0
        ? $"All healthy ({profiles.Count} profiles)."
        : $"{totalIssues} issue(s) across {profiles.Count} profiles.");
    return totalIssues == 0 ? 0 : 2;
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
