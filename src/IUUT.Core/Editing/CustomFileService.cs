using System.Text.Json;
using IUUT.Core.Io;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
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

    /// <summary>Creates the service over the safe writer.</summary>
    public CustomFileService(ISafeSaveWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writer = writer;
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
