using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;
using IUUT.Core.Models;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Orbital Stash builder (master §8.6, §10.4): lists <c>MetaInventory.json</c> items (with stack
/// counts, flagged if a loadout references them), adds catalog items with a fresh GUID and a stack
/// count (capped at the hard game max of <see cref="StashEditService.MaxStack"/>), and removes the
/// selected one — all staged in memory until <b>Apply</b> writes the file via
/// <see cref="CustomFileService"/> (backup + atomic). Edits via <see cref="StashEditService"/>; the
/// loadout coupling via <see cref="LoadoutCrossReference"/>. The confirm lives in the view.
/// </summary>
public sealed class StashViewerViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly StashEditService _stashEdit;
    private readonly LoadoutCrossReference _crossReference;
    private readonly GameCatalogs _catalogs;
    private readonly string _saveFolder;

    private MetaInventoryModel? _stash;
    private HashSet<string> _referenced = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, int> _rowMaxDurability = new(StringComparer.Ordinal);
    private StashItemViewModel? _selectedItem;
    private CatalogRow? _selectedCatalogItem;
    private int _addQuantity = 1;
    private bool _hasChanges;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the builder for one save profile folder.</summary>
    public StashViewerViewModel(
        CustomFileService files,
        StashEditService stashEdit,
        LoadoutCrossReference crossReference,
        GameCatalogs catalogs,
        string saveFolder,
        string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(stashEdit);
        ArgumentNullException.ThrowIfNull(crossReference);
        ArgumentNullException.ThrowIfNull(catalogs);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _files = files;
        _stashEdit = stashEdit;
        _crossReference = crossReference;
        _catalogs = catalogs;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Items = [];
        CatalogItems = catalogs.Items.Rows.OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase).ToList();

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddItemCommand = new RelayCommand(AddItem, () => !IsBusy && _stash is not null && SelectedCatalogItem is not null);
        RemoveSelectedCommand = new RelayCommand(RemoveSelected, () => !IsBusy && _stash is not null && SelectedItem is not null);
        RepairSelectedCommand = new RelayCommand(RepairSelected, () => !IsBusy && _stash is not null && SelectedItem is { HasDurability: true });
        RepairAllCommand = new RelayCommand(RepairAll, () => !IsBusy && _stash is not null);
    }

    /// <summary>The profile being edited (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The current stash items.</summary>
    public ObservableCollection<StashItemViewModel> Items { get; }

    /// <summary>The catalog items available to add (embedded D_ItemsStatic, sorted by label).</summary>
    public IReadOnlyList<CatalogRow> CatalogItems { get; }

    /// <summary>Reloads the save into the builder (discards staged changes).</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Adds the selected catalog item at <see cref="AddQuantity"/> (staged).</summary>
    public IRelayCommand AddItemCommand { get; }

    /// <summary>Removes the selected stash item (staged).</summary>
    public IRelayCommand RemoveSelectedCommand { get; }

    /// <summary>Repairs the selected item to its max durability (staged).</summary>
    public IRelayCommand RepairSelectedCommand { get; }

    /// <summary>Repairs every damaged item to its max durability (staged).</summary>
    public IRelayCommand RepairAllCommand { get; }

    /// <summary>The catalog item to add.</summary>
    public CatalogRow? SelectedCatalogItem
    {
        get => _selectedCatalogItem;
        set
        {
            if (SetProperty(ref _selectedCatalogItem, value))
            {
                AddItemCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>The stack count for the next add, clamped to 1..<see cref="StashEditService.MaxStack"/>.</summary>
    public int AddQuantity
    {
        get => _addQuantity;
        set => SetProperty(ref _addQuantity, Math.Clamp(value, 1, StashEditService.MaxStack));
    }

    /// <summary>The selected stash item (the removal target).</summary>
    public StashItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                RemoveSelectedCommand.NotifyCanExecuteChanged();
                RepairSelectedCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Whether a stash item is selected.</summary>
    public bool HasSelection => SelectedItem is not null;

    /// <summary>Whether there are unsaved staged changes (enables Apply).</summary>
    public bool HasChanges
    {
        get => _hasChanges;
        private set => SetProperty(ref _hasChanges, value);
    }

    /// <summary>Item-count summary.</summary>
    public string Summary => $"{Items.Count:N0} items · {Items.Count(i => i.IsReferenced):N0} referenced by loadouts";

    /// <summary>True while loading or applying.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                AddItemCommand.NotifyCanExecuteChanged();
                RemoveSelectedCommand.NotifyCanExecuteChanged();
                RepairSelectedCommand.NotifyCanExecuteChanged();
                RepairAllCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Status-bar message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>True once <c>MetaInventory.json</c> parsed and the builder is usable.</summary>
    public bool IsLoaded => _stash is not null;

    /// <summary>Loads (or reloads) the stash into the builder, discarding staged changes.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _stash = await _files.LoadStashAsync(_saveFolder);
            if (_stash is null)
            {
                Items.Clear();
                _referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HasChanges = false;
                OnPropertyChanged(nameof(Summary));
                StatusMessage = "Could not load this save's MetaInventory.json (missing or unreadable).";
                return;
            }

            var loadouts = await _files.LoadLoadoutsAsync(_saveFolder);
            _referenced = loadouts is null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(_crossReference.ReferencedDatabaseGuids(loadouts), StringComparer.OrdinalIgnoreCase);

            _rowMaxDurability = ComputeRowMaxDurability(_stash);
            RebuildItems();
            HasChanges = false;
            StatusMessage = $"Loaded {Items.Count} stash item(s) for “{ProfileLabel}”.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not load the save: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
            AddItemCommand.NotifyCanExecuteChanged();
            RemoveSelectedCommand.NotifyCanExecuteChanged();
            RepairSelectedCommand.NotifyCanExecuteChanged();
            RepairAllCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Writes the staged stash to disk (call after a user confirm).</summary>
    public async Task ApplyAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (_stash is null)
        {
            StatusMessage = "Nothing is loaded to apply.";
            return;
        }

        if (!HasChanges)
        {
            StatusMessage = "No staged changes to apply.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _files.SaveStashAsync(_saveFolder, _stash);
            StatusMessage = result.Ok
                ? "Applied the stash to MetaInventory.json — a backup was taken."
                : "Apply failed; the original MetaInventory.json is unchanged.";
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

        await LoadAsync();
    }

    private void AddItem()
    {
        if (_stash is null || SelectedCatalogItem is null)
        {
            return;
        }

        var item = _stashEdit.AddItem(_stash, SelectedCatalogItem.RowName);
        _stashEdit.SetStack(item, AddQuantity);
        RebuildItems();
        HasChanges = true;
        StatusMessage = $"Added {SelectedCatalogItem.Label} ×{Math.Clamp(AddQuantity, 1, StashEditService.MaxStack)} (staged) — Apply to save.";
    }

    private void RemoveSelected()
    {
        if (_stash is null || SelectedItem is null)
        {
            return;
        }

        var item = SelectedItem;
        if (_stashEdit.RemoveItem(_stash, item.DatabaseGuid))
        {
            RebuildItems();
            HasChanges = true;
            StatusMessage = item.IsReferenced
                ? $"Removed {item.Label} (staged) — ⚠ a loadout referenced it (now dangling). Apply to save."
                : $"Removed {item.Label} (staged) — Apply to save.";
        }
    }

    private void RebuildItems()
    {
        Items.Clear();
        if (_stash is not null)
        {
            foreach (var item in _stash.Items)
            {
                var rowName = item.ItemStaticData.RowName;
                var durability = StashEditService.GetDurability(item);
                int? maxDurability = durability is not null && _rowMaxDurability.TryGetValue(rowName, out var max) ? max : null;
                Items.Add(new StashItemViewModel(
                    rowName,
                    _catalogs.Items.Label(rowName),
                    item.DatabaseGuid,
                    StashEditService.GetStack(item),
                    durability,
                    maxDurability,
                    _referenced.Contains(item.DatabaseGuid)));
            }
        }

        SelectedItem = null;
        OnPropertyChanged(nameof(Summary));
    }

    private void RepairSelected()
    {
        var selected = SelectedItem;
        if (_stash is null || selected is null || !selected.HasDurability)
        {
            return;
        }

        var repaired = RepairItems([selected.DatabaseGuid]);
        StatusMessage = repaired > 0
            ? $"Repaired {selected.Label} to full durability (staged) — Apply to save."
            : $"{selected.Label} is already at its max durability.";
    }

    private void RepairAll()
    {
        if (_stash is null)
        {
            return;
        }

        var guids = _stash.Items
            .Where(i => StashEditService.GetDurability(i) is not null)
            .Select(i => i.DatabaseGuid);
        var repaired = RepairItems(guids);
        StatusMessage = repaired > 0
            ? $"Repaired {repaired} item(s) to full durability (staged) — Apply to save."
            : "No items need repair — all are at max durability.";
    }

    /// <summary>Repairs the given items to the max durability seen for their item type; returns how many changed.</summary>
    private int RepairItems(IEnumerable<string> databaseGuids)
    {
        if (_stash is null)
        {
            return 0;
        }

        var targets = new HashSet<string>(databaseGuids, StringComparer.OrdinalIgnoreCase);
        var repaired = 0;
        foreach (var item in _stash.Items)
        {
            if (!targets.Contains(item.DatabaseGuid))
            {
                continue;
            }

            var current = StashEditService.GetDurability(item);
            if (current is null)
            {
                continue;
            }

            if (_rowMaxDurability.TryGetValue(item.ItemStaticData.RowName, out var max) && max > current.Value)
            {
                _stashEdit.SetDurability(item, max);
                repaired++;
            }
        }

        if (repaired > 0)
        {
            RebuildItems();
            HasChanges = true;
        }

        return repaired;
    }

    private static Dictionary<string, int> ComputeRowMaxDurability(MetaInventoryModel stash)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in stash.Items)
        {
            var durability = StashEditService.GetDurability(item);
            if (durability is null)
            {
                continue;
            }

            var row = item.ItemStaticData.RowName;
            if (!map.TryGetValue(row, out var current) || durability.Value > current)
            {
                map[row] = durability.Value;
            }
        }

        return map;
    }
}
