using System.Text.Json;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Validation;

namespace IUUT.Core.Recovery;

/// <summary>
/// Surfaces the corruption causes that a backup-and-atomic-write recovery cannot fix by
/// itself but can warn about (master doc §11.3, Appendix E): Steam Cloud overwrite, cloud-sync
/// conflicted copies, antivirus / Controlled Folder Access write blocks, save-schema
/// incompatibility (DataVersion), and cross-file incoherence after independent restores.
/// Read-only — produces human-readable advisories for the recovery report.
/// </summary>
public sealed class RecoveryAdvisor
{
    /// <summary>The newest <c>Profile.DataVersion</c> IUUT understands (Mendel / Week 220 — Appendix D).</summary>
    public const int KnownDataVersion = 4;

    private const string SteamCloudManifest = "steam_autocloud.vdf";

    /// <summary>Produces advisories for <paramref name="profileFolder"/> given the recovery <paramref name="results"/>.</summary>
    public IReadOnlyList<string> Advise(string profileFolder, IReadOnlyList<RecoveryFileResult> results)
    {
        ArgumentException.ThrowIfNullOrEmpty(profileFolder);
        ArgumentNullException.ThrowIfNull(results);

        var advisories = new List<string>();
        AddSteamCloud(profileFolder, advisories);
        AddConflictedCopies(profileFolder, advisories);
        AddAccessDenied(results, advisories);
        AddDataVersionMismatch(profileFolder, advisories);
        AddCoherence(profileFolder, advisories);
        return advisories;
    }

    private static void AddSteamCloud(string folder, List<string> advisories)
    {
        if (File.Exists(Path.Combine(folder, SteamCloudManifest)))
        {
            advisories.Add(
                "Steam Cloud is configured for this account (steam_autocloud.vdf present). If a recovered " +
                "save reverts after you launch the game, disable Steam Cloud for Icarus before recovering, " +
                "then re-enable it.");
        }
    }

    private static void AddConflictedCopies(string folder, List<string> advisories)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        var conflicts = Directory.EnumerateFiles(folder)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrEmpty(n) && n.Contains("conflicted copy", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (conflicts.Count > 0)
        {
            advisories.Add(
                $"Cloud-sync conflicted copies detected ({string.Join(", ", conflicts)}). OneDrive/Dropbox may " +
                "be syncing your save folder — keep the correct copy and move the folder out of the synced location.");
        }
    }

    private static void AddAccessDenied(IReadOnlyList<RecoveryFileResult> results, List<string> advisories)
    {
        var blocked = results.Any(r => r.Failed && r.Error is not null &&
            (r.Error.Contains("denied", StringComparison.OrdinalIgnoreCase) ||
             r.Error.Contains("UnauthorizedAccess", StringComparison.OrdinalIgnoreCase)));

        if (blocked)
        {
            advisories.Add(
                "A file write was blocked (access denied). Antivirus or Windows Controlled Folder Access may be " +
                "protecting the save folder — allow IUUT under Virus & threat protection → Controlled folder access, then retry.");
        }
    }

    private static void AddDataVersionMismatch(string folder, List<string> advisories)
    {
        var profile = TryParse(Path.Combine(folder, "Profile.json"), ProfileParser.Parse);
        if (profile is not null && profile.DataVersion != 0 && profile.DataVersion != KnownDataVersion)
        {
            advisories.Add(
                $"This save's DataVersion is {profile.DataVersion}, but IUUT knows version {KnownDataVersion}. A save " +
                "that will not load on a newer game build may be a version mismatch (incompatibility), not corruption " +
                "— recovery cannot downgrade it.");
        }
    }

    private static void AddCoherence(string folder, List<string> advisories)
    {
        var profile = TryParse(Path.Combine(folder, "Profile.json"), ProfileParser.Parse);
        var characters = TryParse(Path.Combine(folder, "Characters.json"), CharactersParser.Parse);
        if (profile is null || characters is null)
        {
            return;
        }

        // Independent per-file restores can pull files from different points in time; flag any
        // cross-file inconsistency the ValidationEngine catches (e.g. ChrSlot ≥ NextChrSlot).
        foreach (var issue in ValidationEngine.ValidateCharacters(characters, profile).Issues)
        {
            advisories.Add($"Restored files may be from different points in time — {issue.Message} Review in Custom.");
        }
    }

    private static T? TryParse<T>(string path, Func<string, T> parse)
        where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return parse(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            return null;
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
}
