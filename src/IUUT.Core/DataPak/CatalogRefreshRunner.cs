using IUUT.Catalog;
using IUUT.Core.Services;

namespace IUUT.Core.DataPak;

/// <summary>
/// Orchestrates the runtime catalog self-refresh: locate the installed game's <c>data.pak</c>,
/// decide staleness from its identity stamp, mine + merge via <see cref="CatalogRefreshService"/>,
/// and write the refreshed catalogs to <see cref="AppPaths.CatalogCacheDirectory"/> (which
/// <see cref="Catalog.GameCatalogs.LoadWithCache"/> prefers over the embedded snapshots). Everything
/// is offline and failure-safe: a rejected refresh leaves the cache untouched.
/// </summary>
public sealed class CatalogRefreshRunner
{
    private const string StampFileName = "pak.stamp";

    private readonly AppPaths _paths;

    /// <summary>Creates the runner over the app's state paths.</summary>
    public CatalogRefreshRunner(AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _paths = paths;
    }

    /// <summary>Finds the installed game's <c>data.pak</c> (override → Steam root → libraries).</summary>
    public static string? LocatePak(string? overridePath = null) => DataPakLocator.Resolve(overridePath);

    /// <summary>Whether the cache is stale relative to <paramref name="pakPath"/> (or absent).</summary>
    public bool IsStale(string pakPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(pakPath);
        var stampFile = Path.Combine(_paths.CatalogCacheDirectory, StampFileName);
        if (!File.Exists(stampFile))
        {
            return true;
        }

        try
        {
            return !string.Equals(File.ReadAllText(stampFile), Stamp(pakPath), StringComparison.Ordinal);
        }
        catch (IOException)
        {
            return true;
        }
    }

    /// <summary>
    /// Refreshes the catalog cache from <paramref name="pakPath"/> when stale (or always, when
    /// <paramref name="force"/>). Returns <c>null</c> when already fresh. Refreshed catalogs take
    /// effect on the next <see cref="Catalog.GameCatalogs.LoadWithCache"/> (app start).
    /// </summary>
    public CatalogRefreshResult? RefreshIfStale(string pakPath, bool force = false, IProgress<string>? progress = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(pakPath);
        if (!force && !IsStale(pakPath))
        {
            progress?.Report("Catalog cache is already fresh for this game build.");
            return null;
        }

        // Capture the pak identity BEFORE mining: if Steam replaces the pak mid-refresh, the
        // pre-mine stamp no longer matches the new file, so the next run correctly re-refreshes
        // (a post-mine stamp would mark stale content as fresh).
        var stamp = Stamp(pakPath);
        var version = $"datapak-runtime-{File.GetLastWriteTimeUtc(pakPath):yyyyMMdd-HHmmss}";
        var mined = DataPakMiner.MineFile(pakPath, progress);
        var result = CatalogRefreshService.Refresh(mined, CurrentCatalogJson, version);
        if (!result.Ok)
        {
            return result; // rejected — cache untouched; callers surface the report.
        }

        Directory.CreateDirectory(_paths.CatalogCacheDirectory);
        foreach (var (fileName, json) in result.UpdatedCatalogs)
        {
            var target = Path.Combine(_paths.CatalogCacheDirectory, fileName);
            var temp = target + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, target, overwrite: true);
        }

        File.WriteAllText(Path.Combine(_paths.CatalogCacheDirectory, StampFileName), stamp);
        progress?.Report("Catalog cache refreshed.");
        return result;
    }

    // The merge input: the cached catalog when present (so curated edits accumulate), else embedded.
    // A corrupt cache file self-heals to embedded — otherwise every future refresh would reject on it.
    private string CurrentCatalogJson(string fileName)
    {
        var cached = Path.Combine(_paths.CatalogCacheDirectory, fileName);
        if (File.Exists(cached))
        {
            try
            {
                var text = File.ReadAllText(cached);
                using var _ = System.Text.Json.JsonDocument.Parse(text);
                return text;
            }
            catch (IOException)
            {
                // fall through to embedded
            }
            catch (System.Text.Json.JsonException)
            {
                // corrupt cache — fall through to embedded (the refresh output then replaces it)
            }
        }

        using var stream = CatalogResources.Open(fileName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string Stamp(string pakPath)
    {
        var info = new FileInfo(pakPath);
        return $"{info.FullName}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
    }
}
