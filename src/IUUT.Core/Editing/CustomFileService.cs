using System.Text.Json;
using IUUT.Core.Io;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Prospects.World;
using IUUT.Core.Serializers;

namespace IUUT.Core.Editing;

/// <summary>
/// Load/save for the single text save files the Custom editors target outside the four-file bundle
/// (master §10.4): <c>Mounts.json</c>, <c>MetaInventory.json</c>, and (read-only) <c>Loadout\
/// Loadouts.json</c>. Loads parse the file (returning <c>null</c> when missing or unparseable);
/// saves go through <see cref="ISafeSaveWriter"/> (backup → temp → re-parse → atomic rename), so a
/// bad write is rolled back and the original is never lost. The bundle files use
/// <see cref="CustomApplyService"/> instead; binary files (flags) have their own path.
/// </summary>
public sealed class CustomFileService
{
    /// <summary>The mounts roster file name.</summary>
    public const string MountsFile = "Mounts.json";

    /// <summary>The orbital stash file name.</summary>
    public const string StashFile = "MetaInventory.json";

    /// <summary>The loadouts file path, relative to the save folder.</summary>
    public static readonly string LoadoutsFile = Path.Combine("Loadout", "Loadouts.json");

    private readonly ISafeSaveWriter _writer;
    private readonly BackupManager _backups;

    /// <summary>Creates the service over the safe writer + backup manager (the latter for binary files).</summary>
    public CustomFileService(ISafeSaveWriter writer, BackupManager backups)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(backups);
        _writer = writer;
        _backups = backups;
    }

    /// <summary>Loads <c>Mounts.json</c>; <c>null</c> if missing/unreadable/unparseable.</summary>
    public Task<MountsModel?> LoadMountsAsync(string saveFolder, CancellationToken cancellationToken = default) =>
        LoadJsonAsync(MountsPath(saveFolder), MountsParser.Parse, cancellationToken);

    /// <summary>Safely writes <c>Mounts.json</c> (backup + re-parse + atomic).</summary>
    public Task<SafeSaveResult> SaveMountsAsync(string saveFolder, MountsModel mounts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mounts);
        return _writer.WriteAsync(
            MountsPath(saveFolder),
            MountsSerializer.Serialize(mounts),
            static content => { _ = MountsParser.Parse(content); },
            cancellationToken);
    }

    /// <summary>Loads <c>MetaInventory.json</c>; <c>null</c> if missing/unreadable/unparseable.</summary>
    public Task<MetaInventoryModel?> LoadStashAsync(string saveFolder, CancellationToken cancellationToken = default) =>
        LoadJsonAsync(StashPath(saveFolder), MetaInventoryParser.Parse, cancellationToken);

    /// <summary>Safely writes <c>MetaInventory.json</c> (backup + re-parse + atomic).</summary>
    public Task<SafeSaveResult> SaveStashAsync(string saveFolder, MetaInventoryModel stash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stash);
        return _writer.WriteAsync(
            StashPath(saveFolder),
            MetaInventorySerializer.Serialize(stash),
            static content => { _ = MetaInventoryParser.Parse(content); },
            cancellationToken);
    }

    /// <summary>Loads <c>Loadout\Loadouts.json</c> (read-only); <c>null</c> if missing/unreadable/unparseable.</summary>
    public Task<LoadoutsModel?> LoadLoadoutsAsync(string saveFolder, CancellationToken cancellationToken = default) =>
        LoadJsonAsync(LoadoutsPath(saveFolder), LoadoutsParser.Parse, cancellationToken);

    /// <summary>
    /// Reads the mounts deployed inside each <c>Prospects\*.json</c> world save, grouped per prospect
    /// (only prospects that actually have mounts). These are SEPARATE from the <c>Mounts.json</c>
    /// roster — they are the mounts currently in active prospects, which the flat roster never showed
    /// (issue #19). Read-only; the world blob is never mutated; unreadable prospect files are skipped.
    /// </summary>
    public async Task<IReadOnlyList<ProspectMountGroup>> LoadProspectMountsAsync(
        string saveFolder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        var directory = Path.Combine(saveFolder, "Prospects");
        if (!Directory.Exists(directory))
        {
            return Array.Empty<ProspectMountGroup>();
        }

        var reader = new ProspectMountReader();
        var groups = new List<ProspectMountGroup>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            IReadOnlyList<ProspectMount> mounts;
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
                mounts = reader.ReadBlob(ProspectFileParser.Parse(json).ProspectBlob);
            }
            catch (Exception ex) when (
                ex is JsonException or IOException or FormatException or InvalidDataException or ArgumentException)
            {
                continue; // skip an unreadable / non-prospect file rather than failing the whole load
            }

            if (mounts.Count > 0)
            {
                groups.Add(new ProspectMountGroup(Path.GetFileNameWithoutExtension(file), mounts));
            }
        }

        return groups;
    }

    // --- Engine flags (binary flags_<SteamID>.dat) ----------------------------

    /// <summary>The save folder's <c>flags_*.dat</c> path, or <c>null</c> if none is present.</summary>
    public string? ResolveFlagsPath(string saveFolder)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);
        return Directory.Exists(saveFolder)
            ? Directory.EnumerateFiles(saveFolder, "flags_*.dat").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault()
            : null;
    }

    /// <summary>Loads the binary flags file; <c>null</c> if missing/unreadable/structurally invalid.</summary>
    public async Task<FlagsFileModel?> LoadFlagsAsync(string saveFolder, CancellationToken cancellationToken = default)
    {
        var path = ResolveFlagsPath(saveFolder);
        if (path is null)
        {
            return null;
        }

        try
        {
            return FlagsFileCodec.Read(await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false));
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>
    /// Safely writes the binary flags file: backup → temp byte write → re-decode (validate) → atomic
    /// rename. Returns whether it succeeded (the original is untouched on any failure).
    /// </summary>
    public async Task<bool> SaveFlagsAsync(string saveFolder, FlagsFileModel flags, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(flags);
        var path = ResolveFlagsPath(saveFolder);
        if (path is null)
        {
            return false;
        }

        var bytes = FlagsFileCodec.Write(flags);
        _ = FlagsFileCodec.Read(bytes); // sanity: what we wrote decodes
        var temp = path + ".iuut-tmp";

        try
        {
            if (File.Exists(path))
            {
                _backups.CreateBackup(path);
            }

            await File.WriteAllBytesAsync(temp, bytes, cancellationToken).ConfigureAwait(false);
            _ = FlagsFileCodec.Read(await File.ReadAllBytesAsync(temp, cancellationToken).ConfigureAwait(false)); // validate temp
            File.Move(temp, path, overwrite: true);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (InvalidDataException)
        {
            return false;
        }
        finally
        {
            TryDelete(temp);
        }
    }

    // --- Prospect associations (per-slot AssociatedProspects_Slot_N.json) ------

    /// <summary>The save folder's <c>AssociatedProspects_Slot_*.json</c> files, sorted by name.</summary>
    public IReadOnlyList<string> ResolveAssociatedProspectFiles(string saveFolder)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);
        return Directory.Exists(saveFolder)
            ? Directory.EnumerateFiles(saveFolder, "AssociatedProspects_Slot_*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList()
            : [];
    }

    /// <summary>Loads one <c>AssociatedProspects_Slot_N.json</c> by full path; <c>null</c> if unreadable/unparseable.</summary>
    public Task<AssociatedProspectsModel?> LoadAssociatedProspectsAsync(string filePath, CancellationToken cancellationToken = default) =>
        LoadJsonAsync(filePath, AssociatedProspectsParser.Parse, cancellationToken);

    /// <summary>Safely writes one <c>AssociatedProspects_Slot_N.json</c> (backup + re-parse + atomic).</summary>
    public Task<SafeSaveResult> SaveAssociatedProspectsAsync(string filePath, AssociatedProspectsModel model, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentNullException.ThrowIfNull(model);
        return _writer.WriteAsync(
            filePath,
            AssociatedProspectsSerializer.Serialize(model),
            static content => { _ = AssociatedProspectsParser.Parse(content); },
            cancellationToken);
    }

    // --- Advanced / raw JSON --------------------------------------------------

    /// <summary>The save folder's JSON files (top level + the <c>Loadout</c> subfolder), sorted by name.</summary>
    public IReadOnlyList<string> ListJsonFiles(string saveFolder)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);
        if (!Directory.Exists(saveFolder))
        {
            return [];
        }

        var files = new List<string>(Directory.EnumerateFiles(saveFolder, "*.json"));
        var loadoutDir = Path.Combine(saveFolder, "Loadout");
        if (Directory.Exists(loadoutDir))
        {
            files.AddRange(Directory.EnumerateFiles(loadoutDir, "*.json"));
        }

        return files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Reads any file's text; <c>null</c> if missing/unreadable.</summary>
    public async Task<string?> ReadTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            return await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
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

    /// <summary>Safely writes raw JSON text (backup + JSON re-parse + atomic). Rejects malformed JSON.</summary>
    public Task<SafeSaveResult> SaveJsonTextAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentNullException.ThrowIfNull(content);
        return _writer.WriteAsync(
            filePath,
            content,
            static text =>
            {
                using (JsonDocument.Parse(text))
                {
                }
            },
            cancellationToken);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string MountsPath(string saveFolder)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);
        return Path.Combine(saveFolder, MountsFile);
    }

    private static string StashPath(string saveFolder)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);
        return Path.Combine(saveFolder, StashFile);
    }

    private static string LoadoutsPath(string saveFolder)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);
        return Path.Combine(saveFolder, LoadoutsFile);
    }

    private static async Task<T?> LoadJsonAsync<T>(string path, Func<string, T> parse, CancellationToken cancellationToken)
        where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return parse(text);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
