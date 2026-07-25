using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.GameTuning;
using IUUT.Core.Services;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Game Tuner page (master §20.1, docs/GAME-TUNING.md): reads the client's Engine.ini, shows
/// each tunable as a toggle (or toggle + slider/number box for numbers), and applies the changes.
/// Writes only Engine.ini (never save files), backed up + atomic via the Core service.
/// </summary>
public sealed class GameTunerViewModel : ObservableObject
{
    private readonly GameTuningService _service;
    private readonly GameTuningCatalog _catalog;
    private readonly Services.SaveRootState _saveRootState;
    private int _loadedRootVersion = -1;

    private string _saveRoot;
    private bool _isBusy;
    private string _statusMessage = "Reads the game's Engine.ini.";

    /// <summary>Creates the Game Tuner over the tuning service + catalog.</summary>
    public GameTunerViewModel(GameTuningService service, GameTuningCatalog catalog, Services.SaveRootState saveRootState)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(saveRootState);
        _service = service;
        _catalog = catalog;
        _saveRootState = saveRootState;
        _saveRoot = saveRootState.Current;

        Settings = [];
        // Grouped (Visual FX / Frame Rate / Performance) + searchable — drives the XAML GroupStyle.
        SettingsView = new Services.FilteredView<GameTuningSettingViewModel>(
            Settings,
            static (t, s) => t.Label.Contains(s, StringComparison.OrdinalIgnoreCase)
                          || t.Description.Contains(s, StringComparison.OrdinalIgnoreCase),
            groupBy: nameof(GameTuningSettingViewModel.Group));
        LoadCommand = new RelayCommand(Load);
    }

    /// <summary>The tunable settings (bind lists to <see cref="SettingsView"/>).</summary>
    public ObservableCollection<GameTuningSettingViewModel> Settings { get; }

    /// <summary>Searchable, grouped projection of <see cref="Settings"/>.</summary>
    public Services.FilteredView<GameTuningSettingViewModel> SettingsView { get; }

    /// <summary>(Re)reads Engine.ini.</summary>
    public IRelayCommand LoadCommand { get; }

    /// <summary>The Icarus save root (Engine.ini is under <c>Config\WindowsNoEditor\</c>).</summary>
    public string SaveRoot
    {
        get => _saveRoot;
        set
        {
            if (SetProperty(ref _saveRoot, value))
            {
                OnPropertyChanged(nameof(EngineIniPath)); // the displayed path derives from the root
            }
        }
    }

    /// <summary>True when the shared save root changed since this page last loaded (the view's
    /// Loaded handler reloads on navigation when stale — Settings is never empty after first load,
    /// so a Count==0 guard alone can never re-fire; review finding).</summary>
    public bool IsSaveRootStale => _loadedRootVersion != _saveRootState.Version;

    /// <summary>The resolved Engine.ini path (for display).</summary>
    public string EngineIniPath => GameTuningService.ResolveEngineIniPath(SaveRoot);

    /// <summary>True while applying.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>Status-bar message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Reads the current Engine.ini state into the settings list.</summary>
    public void Load()
    {
        try
        {
            // Follow the root browsed on Home (the VM is a singleton; the view has no root input).
            _loadedRootVersion = _saveRootState.Version;
            SaveRoot = _saveRootState.Current;
            Settings.Clear();
            foreach (var state in _service.ReadCurrent(SaveRoot, _catalog))
            {
                Settings.Add(new GameTuningSettingViewModel(state));
            }

            StatusMessage = $"{Settings.Count(s => s.Enabled)} of {Settings.Count} settings active · {EngineIniPath}";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not read Engine.ini: {ex.Message}";
        }
#pragma warning restore CA1031
    }

    /// <summary>Applies the settings to Engine.ini (call after a user confirm), then refreshes.</summary>
    public async Task ApplyAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var ok = await _service.ApplyAsync(SaveRoot, Settings.Select(s => s.State).ToList());
            StatusMessage = ok
                ? "Applied to Engine.ini — restart Icarus for the changes to take effect."
                : "Apply failed; the original Engine.ini is unchanged.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Apply failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }

        // Refresh from Engine.ini, but keep the apply outcome visible in the status bar.
        var appliedStatus = StatusMessage;
        Load();
        if (Settings.Count > 0)
        {
            StatusMessage = appliedStatus; // only over a healthy reload — a reload FAILURE must stay visible
        }
    }
}
