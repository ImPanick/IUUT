using System.Globalization;
using IUUT.Core.Io;

namespace IUUT.Core.GameTuning;

/// <summary>
/// Reads and applies Game Tuning settings to the client's <c>Engine.ini</c> (docs/GAME-TUNING.md,
/// master §20.1). Writes through <see cref="ISafeSaveWriter"/> — backup → temp → re-parse → atomic
/// rename — so a bad INI (which can stop the game launching) is rolled back; the original is never
/// lost. Does not touch save files. Single-player-first; no network.
/// </summary>
public sealed class GameTuningService
{
    private readonly ISafeSaveWriter _writer;

    /// <summary>Creates the service over the safe writer.</summary>
    public GameTuningService(ISafeSaveWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writer = writer;
    }

    /// <summary>The <c>Engine.ini</c> path for a save root (<c>&lt;saveRoot&gt;\Config\WindowsNoEditor\Engine.ini</c>).</summary>
    public static string ResolveEngineIniPath(string saveRoot)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveRoot);
        return Path.Combine(saveRoot, "Config", "WindowsNoEditor", "Engine.ini");
    }

    /// <summary>Reads the current Engine.ini and reports each catalog setting's state (enabled? value?).</summary>
    public IReadOnlyList<GameTuningState> ReadCurrent(string saveRoot, GameTuningCatalog catalog)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveRoot);
        ArgumentNullException.ThrowIfNull(catalog);

        var ini = Load(ResolveEngineIniPath(saveRoot));
        var states = new List<GameTuningState>(catalog.Settings.Count);
        foreach (var setting in catalog.Settings)
        {
            var raw = ini.GetValue(setting.Section, setting.Key);
            var value = setting.Default;
            if (raw is not null && setting.Kind == GameTuningKind.Number &&
                double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                value = Math.Clamp(parsed, setting.Min, setting.StableMax);
            }

            states.Add(new GameTuningState { Setting = setting, Enabled = raw is not null, Value = value });
        }

        return states;
    }

    /// <summary>
    /// Applies <paramref name="states"/> to Engine.ini: enabled settings write their cvar (numbers
    /// clamped to the stable max), disabled settings are removed. Returns whether the write succeeded.
    /// </summary>
    public async Task<bool> ApplyAsync(
        string saveRoot,
        IReadOnlyList<GameTuningState> states,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveRoot);
        ArgumentNullException.ThrowIfNull(states);

        var path = ResolveEngineIniPath(saveRoot);
        var ini = Load(path);

        foreach (var state in states)
        {
            if (state.Enabled)
            {
                var value = state.Setting.Kind == GameTuningKind.Toggle
                    ? state.Setting.OnValue
                    : FormatNumber(Math.Clamp(state.Value, state.Setting.Min, state.Setting.StableMax));
                ini.SetValue(state.Setting.Section, state.Setting.Key, value);
            }
            else
            {
                ini.RemoveKey(state.Setting.Section, state.Setting.Key);
            }
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var result = await _writer
            .WriteAsync(path, ini.ToText(), static content => { _ = EngineIni.Parse(content); }, cancellationToken)
            .ConfigureAwait(false);
        return result.Ok;
    }

    private static EngineIni Load(string path)
    {
        try
        {
            return EngineIni.Parse(File.Exists(path) ? File.ReadAllText(path) : "");
        }
        catch (IOException)
        {
            return EngineIni.Parse("");
        }
        catch (UnauthorizedAccessException)
        {
            return EngineIni.Parse("");
        }
    }

    private static string FormatNumber(double value) =>
        value % 1 == 0
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.###", CultureInfo.InvariantCulture);
}
