using System.Text.Json;
using IUUT.Core.Io;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.ProspectBlob;
using IUUT.Core.Services;

namespace IUUT.Core.Recovery;

/// <summary>
/// Builds a <see cref="RecoveryPlan"/> for a profile folder (master doc §12.1): health-scans
/// every file, and for each problem file walks its backup chain
/// (<see cref="BackupChainWalker"/>) to pick a clean restore, falling back to template repair
/// (<see cref="TemplateRepairService"/>) or marking it unrecoverable. Pure planning — no writes.
/// Actions come back sorted into the §12.1 restore order.
/// </summary>
public sealed class RecoveryPlanner
{
    private const string ProspectsFolder = "Prospects";

    private readonly HealthScanService _health;
    private readonly BackupChainWalker _walker;
    private readonly TemplateRepairService _templater;

    /// <summary>Creates the planner over the health scanner, backup walker, and template-repair service.</summary>
    public RecoveryPlanner(HealthScanService health, BackupChainWalker walker, TemplateRepairService templater)
    {
        ArgumentNullException.ThrowIfNull(health);
        ArgumentNullException.ThrowIfNull(walker);
        ArgumentNullException.ThrowIfNull(templater);
        _health = health;
        _walker = walker;
        _templater = templater;
    }

    /// <summary>Plans recovery for <paramref name="profileFolder"/> (a <c>PlayerData\&lt;SteamID&gt;</c> directory).</summary>
    public RecoveryPlan Plan(string profileFolder)
    {
        ArgumentException.ThrowIfNullOrEmpty(profileFolder);

        var folderSteamId = Path.GetFileName(Path.TrimEndingDirectorySeparator(profileFolder));
        var report = _health.ScanProfile(profileFolder);

        var actions = new List<RecoveryFileAction>();
        foreach (var file in report.Files)
        {
            if (file.Status == FileHealthStatus.Missing)
            {
                continue; // optional file simply absent — nothing to recover
            }

            var fullPath = Path.Combine(profileFolder, file.RelativePath);
            actions.Add(file.Status == FileHealthStatus.Ok
                ? new RecoveryFileAction { RelativePath = file.RelativePath, FullPath = fullPath, Outcome = RecoveryOutcome.AlreadyOk }
                : PlanForIssue(folderSteamId, file.RelativePath, fullPath));
        }

        actions.Sort((a, b) => RestoreRank(a.RelativePath).CompareTo(RestoreRank(b.RelativePath)));
        return new RecoveryPlan { ProfileFolder = profileFolder, Actions = actions };
    }

    private RecoveryFileAction PlanForIssue(string folderSteamId, string relativePath, string fullPath)
    {
        var isProspect = IsProspect(relativePath);
        var scan = _walker.Scan(fullPath, content => IsClean(relativePath, content), isProspect);

        if (scan.HasCleanCandidate)
        {
            var outcome = scan.ChosenKind == BackupRestoreKind.IuutBackup
                ? RecoveryOutcome.RestoreFromIuutBackup
                : RecoveryOutcome.RestoreFromGameBackup;
            return new RecoveryFileAction
            {
                RelativePath = relativePath,
                FullPath = fullPath,
                Outcome = outcome,
                SourceBackupPath = scan.Chosen!.Path,
                Note = $"Restore from {Path.GetFileName(scan.Chosen.Path)}.",
            };
        }

        // No clean backup → template repair (modeled files) or refuse.
        var repair = _templater.Repair(relativePath, TryReadText(fullPath), folderSteamId);
        return repair.CanRepair
            ? new RecoveryFileAction
            {
                RelativePath = relativePath,
                FullPath = fullPath,
                Outcome = RecoveryOutcome.TemplateRepair,
                RepairedContent = repair.NewContent,
                Note = repair.Notes,
            }
            : new RecoveryFileAction
            {
                RelativePath = relativePath,
                FullPath = fullPath,
                Outcome = RecoveryOutcome.Unrecoverable,
                Note = repair.Notes,
            };
    }

    private static bool IsProspect(string relativePath) =>
        relativePath.Replace('\\', '/').StartsWith(ProspectsFolder + "/", StringComparison.OrdinalIgnoreCase);

    private static bool IsClean(string relativePath, string content)
    {
        try
        {
            if (IsProspect(relativePath))
            {
                return ProspectIsClean(content);
            }

            switch (Path.GetFileName(relativePath))
            {
                case "Profile.json":
                    _ = ProfileParser.Parse(content);
                    return true;
                case "Characters.json":
                    _ = CharactersParser.Parse(content);
                    return true;
                case "Accolades.json":
                    _ = AccoladesParser.Parse(content);
                    return true;
                case "BestiaryData.json":
                    _ = BestiaryParser.Parse(content);
                    return true;
                default:
                    using (JsonDocument.Parse(content))
                    {
                        return true;
                    }
            }
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool ProspectIsClean(string content)
    {
        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.TryGetProperty("ProspectBlob", out var blobElement))
        {
            var blob = blobElement.Deserialize<ProspectBlobModel>(IcarusJson.Options);
            if (blob is not null && !ProspectBlobVerifier.VerifyHash(blob))
            {
                return false;
            }
        }

        return true;
    }

    private static string? TryReadText(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    // Restore order — master doc §12.1.
    private static int RestoreRank(string relativePath)
    {
        var name = Path.GetFileName(relativePath);
        if (name == "Profile.json")
        {
            return 1;
        }

        if (name == "Characters.json")
        {
            return 2;
        }

        if (name is "MetaInventory.json" or "Loadouts.json")
        {
            return 3;
        }

        if (name.StartsWith("AssociatedProspects_", StringComparison.Ordinal))
        {
            return 4;
        }

        return IsProspect(relativePath) ? 5 : 6; // Prospects, then Accolades/Bestiary/Mounts/etc.
    }
}
