using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Field Guide (Tier 4): the tracked statistics, fishing records, and checklists the game
/// keeps but IUUT previously only round-tripped. Stats and fish are catalog-named
/// (<c>D_PlayerTrackers</c>, <c>D_FishData</c>) and fully editable; every fish is listed even
/// when never caught, so the guide shows what's missing. Applies through
/// <see cref="CustomApplyService"/> — backed up, validated, atomic.
/// </summary>
public sealed class FieldGuideViewModel : ObservableObject, Services.IDirtyEditor
{
    private readonly CustomApplyService _apply;
    private readonly FieldGuideEditService _service;
    private readonly GameCatalogs _catalogs;
    private readonly string _saveFolder;

    private SaveEditBundle? _bundle;
    private bool _isBusy;
    private bool _isDirty;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the Field Guide for one save profile folder.</summary>
    public FieldGuideViewModel(
        CustomApplyService apply,
        FieldGuideEditService service,
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

        Stats = [];
        Fish = [];
        TaskLists = [];
        StatsView = new Services.FilteredView<TrackedStatRowViewModel>(
            Stats,
            static (s, q) => s.Label.Contains(q, StringComparison.OrdinalIgnoreCase)
                          || s.RowName.Contains(q, StringComparison.OrdinalIgnoreCase)
                          || s.Category.Contains(q, StringComparison.OrdinalIgnoreCase));
        FishView = new Services.FilteredView<FishRowViewModel>(
            Fish,
            static (f, q) => f.Label.Contains(q, StringComparison.OrdinalIgnoreCase)
                          || f.RowName.Contains(q, StringComparison.OrdinalIgnoreCase));

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        CatchAllFishCommand = new RelayCommand(CatchAllFish, () => !IsBusy && _bundle is not null);
    }

    /// <summary>The profile being edited (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>Every catalog statistic, with the save's value (0 when never recorded).</summary>
    public ObservableCollection<TrackedStatRowViewModel> Stats { get; }

    /// <summary>Every catalog fish, with the save's records (0 when never caught).</summary>
    public ObservableCollection<FishRowViewModel> Fish { get; }

    /// <summary>The checklists the save records.</summary>
    public ObservableCollection<TaskListRowViewModel> TaskLists { get; }

    /// <summary>Searchable projection of <see cref="Stats"/>.</summary>
    public Services.FilteredView<TrackedStatRowViewModel> StatsView { get; }

    /// <summary>Searchable projection of <see cref="Fish"/>.</summary>
    public Services.FilteredView<FishRowViewModel> FishView { get; }

    /// <summary>Reloads the save into the guide.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Marks every uncaught fish as caught once (review, then Apply).</summary>
    public IRelayCommand CatchAllFishCommand { get; }

    /// <summary>Header summary.</summary>
    public string Summary =>
        $"{Stats.Count(s => s.Value > 0):N0} of {Stats.Count:N0} stats recorded · " +
        $"{Fish.Count(f => f.IsCaught):N0} of {Fish.Count:N0} fish caught · " +
        $"{TaskLists.Count:N0} checklist(s)";

    /// <summary>True while loading or applying.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                CatchAllFishCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Status-bar message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>True once the save's files parsed and the guide is usable.</summary>
    public bool IsLoaded => _bundle is not null;

    /// <inheritdoc />
    public bool IsDirty
    {
        get => _isDirty;
        private set => SetProperty(ref _isDirty, value);
    }

    /// <summary>Loads (or reloads) the save into the guide.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _bundle = await _apply.LoadAsync(_saveFolder);
            ClearRows();

            if (_bundle is null)
            {
                StatusMessage = "Could not load this save's Accolades.json / BestiaryData.json (missing or unreadable).";
                return;
            }

            BuildStats(_bundle);
            BuildFish(_bundle);
            BuildTaskLists(_bundle);

            OnPropertyChanged(nameof(Summary));
            StatusMessage = $"Loaded the field guide for “{ProfileLabel}”.";
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
            CatchAllFishCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Applies the edited guide (call after a user confirm).</summary>
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

        IsBusy = true;
        try
        {
            foreach (var stat in Stats)
            {
                _service.SetStat(_bundle.Accolades, stat.RowName, stat.Value);
            }

            foreach (var fish in Fish.Where(f => f.IsCaught))
            {
                _service.SetFish(_bundle.Bestiary, fish.RowName, fish.Caught, fish.MaxQuality, fish.MaxWeight, fish.MaxLength);
            }

            foreach (var task in TaskLists.SelectMany(l => l.Tasks))
            {
                _service.SetTaskCompleted(_bundle.Accolades, task.ListRowName, task.Task, task.IsCompleted);
            }

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
                StatusMessage = "No changes to apply.";
                return;
            }

            var report = await _apply.ApplyAsync(plan);
            StatusMessage = report.Applied
                ? $"Applied the field guide — {report.Message} A backup was taken."
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

    private void BuildStats(SaveEditBundle bundle)
    {
        var recorded = _service.ListStats(bundle.Accolades)
            .ToDictionary(s => s.RowName, s => s.Value, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in _catalogs.PlayerTrackers.Rows.OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase))
        {
            var category = row.Extra is { } extra && extra.TryGetValue("category", out var c) &&
                           c.ValueKind == System.Text.Json.JsonValueKind.String
                ? c.GetString() ?? ""
                : "";
            AddStat(new TrackedStatRowViewModel(row.RowName, row.Label, category, recorded.GetValueOrDefault(row.RowName)));
            seen.Add(row.RowName);
        }

        // A stat the save records but the catalog doesn't know is still editable.
        foreach (var (rowName, value) in recorded)
        {
            if (seen.Add(rowName))
            {
                AddStat(new TrackedStatRowViewModel(rowName, CatalogName.Humanize(rowName), "", value));
            }
        }
    }

    private void BuildFish(SaveEditBundle bundle)
    {
        var caught = bundle.Bestiary.FishTracking
            .Where(f => !string.IsNullOrEmpty(f.FishRow.RowName))
            .GroupBy(f => f.FishRow.RowName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in _catalogs.Fish.Rows.OrderBy(r => r.RowName, StringComparer.OrdinalIgnoreCase))
        {
            var lore = row.Extra is { } extra && extra.TryGetValue("lore", out var l) &&
                       l.ValueKind == System.Text.Json.JsonValueKind.String
                ? l.GetString()
                : null;
            var record = caught.GetValueOrDefault(row.RowName);
            AddFish(new FishRowViewModel(
                row.RowName, row.Label, lore,
                record?.CaughtCount ?? 0, record?.MaxQuality ?? 0, record?.MaxWeight ?? 0, record?.MaxLength ?? 0));
            seen.Add(row.RowName);
        }

        foreach (var (rowName, record) in caught)
        {
            if (seen.Add(rowName))
            {
                AddFish(new FishRowViewModel(
                    rowName, CatalogName.Humanize(rowName), null,
                    record.CaughtCount, record.MaxQuality, record.MaxWeight, record.MaxLength));
            }
        }
    }

    private void BuildTaskLists(SaveEditBundle bundle)
    {
        foreach (var list in _service.ListTaskLists(bundle.Accolades))
        {
            var tasks = list.CompletedTasks
                .Select(t =>
                {
                    var row = new TaskRowViewModel(list.RowName, t);
                    row.PropertyChanged += OnRowChanged;
                    return row;
                })
                .ToList();
            TaskLists.Add(new TaskListRowViewModel(list.RowName, CatalogName.Humanize(list.RowName), tasks));
        }
    }

    private void AddStat(TrackedStatRowViewModel row)
    {
        row.PropertyChanged += OnRowChanged;
        Stats.Add(row);
    }

    private void AddFish(FishRowViewModel row)
    {
        row.PropertyChanged += OnRowChanged;
        Fish.Add(row);
    }

    private void ClearRows()
    {
        foreach (var row in Stats)
        {
            row.PropertyChanged -= OnRowChanged;
        }

        foreach (var row in Fish)
        {
            row.PropertyChanged -= OnRowChanged;
        }

        foreach (var task in TaskLists.SelectMany(l => l.Tasks))
        {
            task.PropertyChanged -= OnRowChanged;
        }

        Stats.Clear();
        Fish.Clear();
        TaskLists.Clear();
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        IsDirty = true;
        OnPropertyChanged(nameof(Summary));
    }

    private void CatchAllFish()
    {
        foreach (var fish in Fish.Where(f => !f.IsCaught))
        {
            fish.Caught = 1;
        }

        StatusMessage = "Marked every uncaught fish as caught once — review, then Apply.";
    }
}
