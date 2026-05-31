using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Services;

namespace IUUT.App.ViewModels;

/// <summary>
/// Binding-shape for the Home screen (master doc §10.2). Holds no domain logic — it
/// drives <see cref="HomeService"/> and exposes its <see cref="HomeState"/> for the view
/// (CODE_STYLE §7). Save mutation (Lazy Max apply) is deferred to the WP-14 pipeline.
/// </summary>
public sealed class HomeViewModel : ObservableObject
{
    private readonly HomeService _home;

    private string _saveRoot;
    private bool _isLoading;
    private bool _saveRootFound;
    private HomeSaveSlot? _selectedSlot;
    private bool _gameRunning;
    private string _gameStatus = "Game state unknown — reload to scan.";
    private string _statusMessage = "Ready.";

    /// <summary>Creates the Home view-model over the Home orchestration service.</summary>
    public HomeViewModel(HomeService home)
    {
        ArgumentNullException.ThrowIfNull(home);
        _home = home;
        _saveRoot = HomeService.DefaultSaveRoot;

        Slots = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        LazyMaxCommand = new RelayCommand(OnLazyMax, () => SelectedSlot is not null);
    }

    /// <summary>Discovered save profiles for the dropdown.</summary>
    public ObservableCollection<HomeSaveSlot> Slots { get; }

    /// <summary>Reloads the Home state from <see cref="SaveRoot"/>.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Entry point to Lazy Max for the selected profile (apply lands in WP-14).</summary>
    public IRelayCommand LazyMaxCommand { get; }

    /// <summary>The save root being scanned (editable; Browse updates it).</summary>
    public string SaveRoot
    {
        get => _saveRoot;
        set => SetProperty(ref _saveRoot, value);
    }

    /// <summary>True while a load is in flight (drives the busy indicator / disables actions).</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    /// <summary>Whether the current <see cref="SaveRoot"/> contains a <c>PlayerData\</c> folder.</summary>
    public bool SaveRootFound
    {
        get => _saveRootFound;
        private set => SetProperty(ref _saveRootFound, value);
    }

    /// <summary>The profile selected in the dropdown.</summary>
    public HomeSaveSlot? SelectedSlot
    {
        get => _selectedSlot;
        set
        {
            if (SetProperty(ref _selectedSlot, value))
            {
                LazyMaxCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Whether Icarus is currently running (drives the warn-only banner colour).</summary>
    public bool GameRunning
    {
        get => _gameRunning;
        private set => SetProperty(ref _gameRunning, value);
    }

    /// <summary>Human-readable game-state line (master doc §14 — warn only, never blocks).</summary>
    public string GameStatus
    {
        get => _gameStatus;
        private set => SetProperty(ref _gameStatus, value);
    }

    /// <summary>Status-bar message (load result, hints, errors).</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Loads profiles + names + game state for <see cref="SaveRoot"/> into the view-model.</summary>
    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var state = await _home.LoadAsync(SaveRoot);

            Slots.Clear();
            foreach (var slot in state.Slots)
            {
                Slots.Add(slot);
            }

            SaveRootFound = state.SaveRootFound;
            SelectedSlot = Slots.Count > 0 ? Slots[0] : null;
            GameRunning = state.Game.IsRunning;
            GameStatus = state.Game.IsRunning
                ? $"Icarus is running ({string.Join(", ", state.Game.MatchedProcessNames)}) — stay on the Main Menu, or close it, before saving."
                : "Icarus is not running (safest).";
            StatusMessage = state.SaveRootFound
                ? $"{Slots.Count} save profile(s) found."
                : "No PlayerData folder here — use Browse to pick your Icarus “Saved” folder.";
        }
#pragma warning disable CA1031 // UI boundary: any failure must surface as a message, never crash the shell.
        catch (Exception ex)
        {
            Slots.Clear();
            SelectedSlot = null;
            SaveRootFound = false;
            StatusMessage = $"Could not load saves: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsLoading = false;
        }
    }

    private void OnLazyMax() =>
        StatusMessage = SelectedSlot is null
            ? "Select a save profile first."
            : $"Lazy Max for “{SelectedSlot.DisplayLabel}” — preview & apply arrives in WP-14.";
}
