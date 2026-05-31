using System.Text.Json;
using IUUT.Core.Io;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.ProspectBlob;

namespace IUUT.Core.Services;

/// <summary>
/// Scans a profile folder and reports per-file health (master doc §11.3 F-020): every
/// known JSON file is parse-checked, and each prospect world file additionally has its
/// blob SHA-1 verified. Read-only — it never writes or repairs (that is RecoveryService).
/// </summary>
public sealed class HealthScanService
{
    private const string ProspectsFolder = "Prospects";

    /// <summary>Scans <paramref name="profileFolder"/> (a <c>PlayerData\&lt;SteamID&gt;</c> directory).</summary>
    public HealthReport ScanProfile(string profileFolder)
    {
        ArgumentException.ThrowIfNullOrEmpty(profileFolder);

        var files = new List<FileHealth>
        {
            CheckJson(profileFolder, "Profile.json", static s => ProfileParser.Parse(s)),
            CheckJson(profileFolder, "Characters.json", static s => CharactersParser.Parse(s)),
            CheckJson(profileFolder, "Accolades.json", static s => AccoladesParser.Parse(s)),
            CheckJson(profileFolder, "BestiaryData.json", static s => BestiaryParser.Parse(s)),
            CheckJson(profileFolder, "MetaInventory.json", RequireJson),
            CheckJson(profileFolder, "Mounts.json", RequireJson),
            CheckJson(profileFolder, Path.Combine("Loadout", "Loadouts.json"), RequireJson),
        };

        // Per-character prospect indices (nested-stringified) — valid-JSON check.
        foreach (var path in GlobJson(profileFolder, "AssociatedProspects_Slot_*.json"))
        {
            files.Add(CheckJsonFile(profileFolder, path, RequireJson));
        }

        // World saves — valid JSON + blob SHA-1 verification.
        foreach (var path in GlobJson(Path.Combine(profileFolder, ProspectsFolder), "*.json"))
        {
            files.Add(CheckProspect(profileFolder, path));
        }

        return new HealthReport { Files = files };
    }

    private static void RequireJson(string content)
    {
        using var _ = JsonDocument.Parse(content);
    }

    private static IEnumerable<string> GlobJson(string directory, string pattern) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, pattern)
            : [];

    private static FileHealth CheckJson(string root, string relativePath, Action<string> validate) =>
        CheckJsonFile(root, Path.Combine(root, relativePath), validate);

    private static FileHealth CheckJsonFile(string root, string fullPath, Action<string> validate)
    {
        var relative = Path.GetRelativePath(root, fullPath);

        if (!File.Exists(fullPath))
        {
            return new FileHealth(relative, FileHealthStatus.Missing, null);
        }

        if (!TryReadAllText(fullPath, out var content, out var readError))
        {
            return new FileHealth(relative, FileHealthStatus.Unreadable, readError);
        }

        try
        {
            validate(content);
            return new FileHealth(relative, FileHealthStatus.Ok, null);
        }
        catch (JsonException ex)
        {
            return new FileHealth(relative, FileHealthStatus.Unparseable, ex.Message);
        }
    }

    private static FileHealth CheckProspect(string root, string fullPath)
    {
        var relative = Path.GetRelativePath(root, fullPath);

        if (!TryReadAllText(fullPath, out var content, out var readError))
        {
            return new FileHealth(relative, FileHealthStatus.Unreadable, readError);
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("ProspectBlob", out var blobElement))
            {
                var blob = blobElement.Deserialize<ProspectBlobModel>(IcarusJson.Options);
                if (blob is not null && !ProspectBlobVerifier.VerifyHash(blob))
                {
                    return new FileHealth(relative, FileHealthStatus.BlobHashMismatch, "prospect blob SHA-1 mismatch");
                }
            }

            return new FileHealth(relative, FileHealthStatus.Ok, null);
        }
        catch (JsonException ex)
        {
            return new FileHealth(relative, FileHealthStatus.Unparseable, ex.Message);
        }
    }

    private static bool TryReadAllText(string path, out string content, out string? error)
    {
        try
        {
            content = File.ReadAllText(path);
            error = null;
            return true;
        }
        catch (IOException ex)
        {
            content = "";
            error = ex.Message;
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            content = "";
            error = ex.Message;
            return false;
        }
    }
}
