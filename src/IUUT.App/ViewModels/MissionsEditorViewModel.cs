using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;
using IUUT.Core.Services;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Missions checklist (Tier 2, master §8.8): every catalog mission with its completion
/// state; staging a mission completes it AND its full prerequisite closure through
/// <see cref="MissionCompletionService"/> at apply (backed up + atomic). Additive only —
/// completed missions are never revoked here.
/// </summary>
public sealed class MissionsEditorViewModel : ObservableObject, Services.IDirtyEditor
{
    private readonly CustomApplyService _apply;
    private readonly MissionCompletionService _service;
    private readonly GameCatalogs _catalogs;
    private readonly string _saveFolder;

    private SaveEditBundle? _bundle;
    private bool _isBusy;
    private bool _isDirty;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the editor for one save profile folder.</summary>
    public MissionsEditorViewModel(
        CustomApplyService apply,
        MissionCompletionService service,
        GameCatalogs catalogs,
        string saveFolder,
        string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(catalogs);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _apply = apply;
        _service = service;
        _catalogs = catalogs;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Missions = [];
        MissionsView = new Services.FilteredView<MissionRowViewModel>(
            Missions,
            static (m, s) => m.Label.Contains(s, StringComparison.OrdinalIgnoreCase)
                          || m.RowName.Contains(s, StringComparison.OrdinalIgnoreCase)
                          || m.Tree.Contains(s, StringComparison.OrdinalIgnoreCase));
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        StageAllCommand = new RelayCommand(StageAll, () => !IsBusy && _bundle is not null);
    }

    /// <summary>The profile being edited (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>Every catalog mission with completion + staging state.</summary>
    public ObservableCollection<MissionRowViewModel> Missions { get; }

    /// <summary>Searchable projection of <see cref="Missions"/>.</summary>
    public Services.FilteredView<MissionRowViewModel> MissionsView { get; }

    /// <summary>Reloads the save into the editor.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Stages every incomplete mission (review, then Apply).</summary>
    public IRelayCommand StageAllCommand { get; }

    /// <summary>Live "complete / staged of total" header summary.</summary>
    public string Summary =>
        $"{Missions.Count(m => m.IsComplete):N0} of {Missions.Count:N0} complete · {Missions.Count(m => m.IsStaged):N0} staged";

    /// <summary>True while loading or applying.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                StageAllCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Status-bar message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>True once the save's Profile.json parsed and the editor is usable.</summary>
    public bool IsLoaded => _bundle is not null;

    /// <inheritdoc />
    public bool IsDirty
    {
        get => _isDirty;
        private set => SetProperty(ref _isDirty, value);
    }

    /// <summary>Loads (or reloads) the mission checklist from the save.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _bundle = await _apply.LoadAsync(_saveFolder);
            Missions.Clear();

            if (_bundle is null)
            {
                StatusMessage = "Could not load this save's Profile.json (missing or unreadable).";
                return;
            }

            var owned = new HashSet<string>(
                _bundle.Profile.Talents.Where(t => !string.IsNullOrEmpty(t.RowName)).Select(t => t.RowName),
                StringComparer.Ordinal);

            foreach (var mission in _catalogs.Missions.Missions
                .OrderBy(m => MissionCatalog.TreeLabel(m.Tree), StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => MissionCatalog.Label(m.RowName), StringComparer.OrdinalIgnoreCase))
            {
                var row = new MissionRowViewModel(
                    mission.RowName,
                    MissionCatalog.Label(mission.RowName),
                    MissionCatalog.TreeLabel(mission.Tree),
                    _catalogs.Missions.AllPrerequisites(mission.RowName).Count,
                    owned.Contains(mission.RowName));
                row.PropertyChanged += OnRowChanged;
                Missions.Add(row);
            }

            OnPropertyChanged(nameof(Summary));
            StatusMessage = $"Loaded {Missions.Count} missions for “{ProfileLabel}”.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not load the save: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsDirty = false; // freshly loaded state is clean
            IsBusy = false;
            StageAllCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Applies the staged completions (call after a user confirm).</summary>
    public async Task ApplyAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (_bundle is null)
        {
            StatusMessage = "Nothing is loaded to apply.";
            return;
        }

        var staged = Missions.Where(m => m.IsStaged).Select(m => m.RowName).ToList();
        if (staged.Count == 0)
        {
            StatusMessage = "No missions staged to complete.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = _service.Complete(_bundle.Profile, staged);

            var plan = await _apply.PreviewBundleAsync(_saveFolder, _bundle);
            if (!plan.CanApply)
            {
                var first = plan.Validation.Errors.FirstOrDefault();
                StatusMessage = first is null
                    ? "Cannot apply: the save did not validate."
                    : $"Cannot apply: {first.Message}";
                return;
            }

            if (!plan.HasChanges)
            {
                StatusMessage = "No changes to apply (everything staged was already complete).";
                return;
            }

            var report = await _apply.ApplyAsync(plan);
            StatusMessage = report.Applied
                ? $"Completed {result.MissionsRequested} mission(s) (+{result.TalentsAdded} unlocks incl. prerequisites) — {report.Message} A backup was taken."
                : $"Apply failed: {report.Message}";
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

        // Reload from disk, but keep the apply outcome visible in the status bar.
        var appliedStatus = StatusMessage;
        await LoadAsync();
        if (IsLoaded)
        {
            StatusMessage = appliedStatus; // only over a healthy reload — a reload FAILURE must stay visible
        }
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MissionRowViewModel.IsStaged))
        {
            OnPropertyChanged(nameof(Summary));
            IsDirty = Missions.Any(m => m.IsStaged);
        }
    }

    private void StageAll()
    {
        foreach (var mission in Missions)
        {
            if (!mission.IsComplete)
            {
                mission.IsStaged = true;
            }
        }

        StatusMessage = "Staged every incomplete mission — review, then Apply.";
    }
}
