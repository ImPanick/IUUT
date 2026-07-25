// IUUT.Cli — headless entry point for scripting and CI (master doc §6.2).
// Hand-rolled verb dispatch: the CLI's needs do not clear the dependency gate
// (SCOPE_GUARDRAILS §2.6) for an argument-parsing package.

using System.Globalization;
using IUUT.Core.Abstractions;
using IUUT.Core.Catalog;
using IUUT.Core.DataPak;
using IUUT.Core.Io;
using IUUT.Core.Services;

if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
{
    PrintUsage();
    return 0;
}

var options = ParseOptions(args);

try
{
    return args[0] switch
    {
        "check" => Check(options),
        "backup-all" => BackupAll(options),
        "lazy-max" => await LazyMaxAsync(options).ConfigureAwait(false),
        "catalog-refresh" => CatalogRefresh(options),
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
    Console.WriteLine("  recover          Guided save recovery lives in the IUUT app — it needs the UI.");
    Console.WriteLine();
    Console.WriteLine("  --root defaults to %LOCALAPPDATA%\\Icarus\\Saved.");
}

static Dictionary<string, string?> ParseOptions(string[] args)
{
    var options = new Dictionary<string, string?>(StringComparer.Ordinal);
    for (var i = 1; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"unexpected argument '{args[i]}' (options start with --)");
        }

        var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
        options[args[i]] = hasValue ? args[++i] : null;
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

    foreach (var profile in profiles)
    {
        foreach (var file in Directory.EnumerateFiles(profile.FolderPath, "*", SearchOption.AllDirectories))
        {
            if (file.Contains(BackupManager.BackupInfix, StringComparison.Ordinal))
            {
                continue; // never back up a backup
            }

            backups.CreateBackup(file);
            count++;
        }

        Console.WriteLine($"{profile.SteamId64}: backed up");
    }

    Console.WriteLine($"{count} file(s) backed up across {profiles.Count} profiles.");
    return 0;
}

static async Task<int> LazyMaxAsync(Dictionary<string, string?> options)
{
    var target = options.GetValueOrDefault("--profile")
        ?? throw new ArgumentException("lazy-max requires --profile <steamid-or-path>");
    var folder = Directory.Exists(target)
        ? target
        : Path.Combine(ResolveRoot(options), SaveDiscoveryService.PlayerDataFolder, target);
    if (!Directory.Exists(folder))
    {
        throw new ArgumentException($"profile folder not found: '{folder}'");
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
            + $"{r.MissionFlagsSet} mission flags — {plan.Files.Count} file(s) would change.");
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
    var pak = CatalogRefreshRunner.LocatePak(options.GetValueOrDefault("--pak"));
    if (pak is null)
    {
        Console.Error.WriteLine("data.pak not found (pass --pak <path> or install the game via Steam).");
        return 1;
    }

    var runner = new CatalogRefreshRunner(AppPaths.Resolve());
    var progress = new Progress<string>(Console.WriteLine);
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
