namespace IUUT.Core.Services;

/// <summary>
/// Composes the Home screen's data (master doc §10.2): discovers save profiles under a
/// save root (<see cref="SaveDiscoveryService"/>), resolves their PersonaNames
/// (<see cref="SteamProfileResolverService"/>, offline-first), and scans for a running
/// Icarus process (<see cref="GameProcessDetector"/>) for the warn-only banner. Pure
/// orchestration — all the real work lives in the three injected services; this only
/// joins their results into a <see cref="HomeState"/> the UI can bind.
/// </summary>
public sealed class HomeService
{
    private readonly SaveDiscoveryService _discovery;
    private readonly SteamProfileResolverService _resolver;
    private readonly GameProcessDetector _gameDetector;

    /// <summary>Creates the Home orchestrator over the discovery, name-resolver, and game-detector services.</summary>
    public HomeService(
        SaveDiscoveryService discovery,
        SteamProfileResolverService resolver,
        GameProcessDetector gameDetector)
    {
        ArgumentNullException.ThrowIfNull(discovery);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(gameDetector);

        _discovery = discovery;
        _resolver = resolver;
        _gameDetector = gameDetector;
    }

    /// <summary>The default save root (<c>%LOCALAPPDATA%\Icarus\Saved</c>).</summary>
    public static string DefaultSaveRoot => SaveDiscoveryService.ResolveDefaultSaveRoot();

    /// <summary>
    /// Loads the Home state for <paramref name="saveRoot"/>: detects the game, discovers
    /// profiles, and resolves their display names. Returns an empty slot list (with
    /// <see cref="HomeState.SaveRootFound"/> = false) when the root has no <c>PlayerData\</c>.
    /// </summary>
    public async Task<HomeState> LoadAsync(string saveRoot, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveRoot);

        // Game detection is independent of the save root, so it always runs.
        var game = _gameDetector.Detect();
        var saveRootFound = _discovery.SaveRootContainsPlayerData(saveRoot);
        var profiles = _discovery.DiscoverProfiles(saveRoot);

        if (profiles.Count == 0)
        {
            return new HomeState
            {
                SaveRoot = saveRoot,
                SaveRootFound = saveRootFound,
                Slots = [],
                Game = game,
            };
        }

        var displays = await _resolver
            .ResolveAllAsync(profiles.Select(p => p.SteamId64), cancellationToken)
            .ConfigureAwait(false);

        // Map by SteamID64 so slot/display pairing is robust to ordering (folder names are unique).
        var displayById = new Dictionary<string, SteamProfileDisplay>(StringComparer.Ordinal);
        foreach (var display in displays)
        {
            displayById[display.SteamId64] = display;
        }

        var slots = profiles
            .Select(p => HomeSaveSlot.From(
                p,
                displayById.TryGetValue(p.SteamId64, out var d)
                    ? d
                    : new SteamProfileDisplay(p.SteamId64, null, SteamNameSource.Fallback, null)))
            .ToList();

        return new HomeState
        {
            SaveRoot = saveRoot,
            SaveRootFound = saveRootFound,
            Slots = slots,
            Game = game,
        };
    }
}
