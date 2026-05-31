using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Services;

namespace IUUT.App.ViewModels;

/// <summary>
/// Binding-shape for the Home screen (master doc §10.2). Holds no domain logic — it
/// drives <see cref="HomeService"/> + <see cref="LazyMaxApplyService"/> and exposes their
/// results for the view (CODE_STYLE §7). The confirmation dialog itself lives in the view
/// (it is UI), so this stays free of WPF references.
/// </summary>
public sealed class HomeViewModel : ObservableObject
{
    private readonly HomeService _home;
    private readonly LazyMaxApplyService _apply;

    private string _saveRoot;
    private bool _isBusy;
    private bool _saveRootFound;
    private HomeSaveSlot? _selectedSlot;
    private bool _gameRunning;
    private string _gameStatus = "Game state unknown — reload to scan.";
    private string _statusMessage = "Ready.";

    /// <summary>Creates the Home view-model over the Home and Lazy Max apply services.</summary>
    public HomeViewModel(HomeService home, LazyMaxApplyService apply)
    {
        ArgumentNullException.ThrowIfNull(home);
        ArgumentNullException.ThrowIfNull(apply);
        _home = home;
        _apply = apply;
        _saveRoot = HomeService.DefaultSaveRoot;

        Slots = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    /// <summary>Discovered save profiles for the dropdown.</summary>
    public ObservableCollection<HomeSaveSlot> Slots { get; }

    /// <summary>Reloads the Home state from <see cref="SaveRoot"/>.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>The save root being scanned (editable; Browse updates it).</summary>
    public string SaveRoot
    {
        get => _saveRoot;
        set => SetProperty(ref _saveRoot, value);
    }

    /// <summary>True while a load or apply is in flight (drives the busy indicator / disables actions).</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
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
        set => SetProperty(ref _selectedSlot, value);
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
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
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
            IsBusy = false;
        }
    }

    /// <summary>
    /// Builds (but does not apply) a Lazy Max plan for the selected profile. The view shows the
    /// plan for confirmation, then calls <see cref="ApplyAsync"/>. Returns <c>null</c> when no
    /// profile is selected or the preview failed.
    /// </summary>
    public async Task<LazyMaxPlan?> PreviewSelectedAsync()
    {
        if (SelectedSlot is null)
        {
            StatusMessage = "Select a save profile first.";
            return null;
        }

        if (IsBusy)
        {
            return null;
        }

        IsBusy = true;
        try
        {
            var plan = await _apply.PreviewAsync(SelectedSlot.FolderPath);
            StatusMessage = plan.CanApply
                ? $"Lazy Max preview ready for “{SelectedSlot.DisplayLabel}” — confirm to apply."
                : "Cannot apply Lazy Max: " + string.Join("; ", plan.Validation.Errors.Select(error => error.Message));
            return plan;
        }
#pragma warning disable CA1031 // UI boundary: any failure must surface as a message, never crash the shell.
        catch (Exception ex)
        {
            StatusMessage = $"Lazy Max preview failed: {ex.Message}";
            return null;
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Applies a confirmed <paramref name="plan"/> and refreshes the Home state afterwards.</summary>
    public async Task ApplyAsync(LazyMaxPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var report = await _apply.ApplyAsync(plan);
            StatusMessage = report.Message ?? (report.Applied ? "Lazy Max applied." : "Lazy Max failed.");
        }
#pragma warning disable CA1031 // UI boundary: any failure must surface as a message, never crash the shell.
        catch (Exception ex)
        {
            StatusMessage = $"Lazy Max apply failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }

        // Reflect the new character counts / state.
        await LoadAsync();
    }

    /// <summary>Sets the status bar to a user-cancelled message (called by the view when the confirm dialog is dismissed).</summary>
    public void NotifyCancelled() => StatusMessage = "Lazy Max cancelled — nothing was changed.";
}
