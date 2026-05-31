using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;
using IUUT.Core.Models;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Orbital Stash viewer (master §8.6, §10.4): lists <c>MetaInventory.json</c> items (flagged if
/// a loadout references them) and removes the selected one through <see cref="StashEditService"/>,
/// writing via <see cref="CustomFileService"/> (backed up + atomic). Adding items is deferred until
/// the catalog is enriched with a friendly picker (items.json). The confirm lives in the view.
/// </summary>
public sealed class StashViewerViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly StashEditService _stashEdit;
    private readonly LoadoutCrossReference _crossReference;
    private readonly GameCatalogs _catalogs;
    private readonly string _saveFolder;

    private MetaInventoryModel? _stash;
    private StashItemViewModel? _selectedItem;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the viewer for one save profile folder.</summary>
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
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    /// <summary>The profile being viewed (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The stash items.</summary>
    public ObservableCollection<StashItemViewModel> Items { get; }

    /// <summary>Reloads the save into the viewer.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>The selected item (the removal target).</summary>
    public StashItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OnPropertyChanged(nameof(HasSelection));
            }
        }
    }

    /// <summary>Whether an item is selected (enables Remove).</summary>
    public bool HasSelection => SelectedItem is not null;

    /// <summary>Item-count summary.</summary>
    public string Summary => $"{Items.Count:N0} items · {Items.Count(i => i.IsReferenced):N0} referenced by loadouts";

    /// <summary>True while loading or applying.</summary>
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

    /// <summary>True once <c>MetaInventory.json</c> parsed and the viewer is usable.</summary>
    public bool IsLoaded => _stash is not null;

    /// <summary>Loads (or reloads) the stash into the viewer.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _stash = await _files.LoadStashAsync(_saveFolder);
            Items.Clear();

            if (_stash is null)
            {
                StatusMessage = "Could not load this save's MetaInventory.json (missing or unreadable).";
                OnPropertyChanged(nameof(Summary));
                return;
            }

            var loadouts = await _files.LoadLoadoutsAsync(_saveFolder);
            var referenced = loadouts is null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(_crossReference.ReferencedDatabaseGuids(loadouts), StringComparer.OrdinalIgnoreCase);

            foreach (var item in _stash.Items)
            {
                var rowName = item.ItemStaticData.RowName;
                Items.Add(new StashItemViewModel(
                    rowName,
                    _catalogs.Items.Label(rowName),
                    item.DatabaseGuid,
                    referenced.Contains(item.DatabaseGuid)));
            }

            OnPropertyChanged(nameof(Summary));
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
        }
    }

    /// <summary>Removes the selected item (call after a user confirm).</summary>
    public async Task RemoveSelectedAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (_stash is null || SelectedItem is null)
        {
            StatusMessage = "Select an item to remove.";
            return;
        }

        var guid = SelectedItem.DatabaseGuid;
        IsBusy = true;
        try
        {
            if (!_stashEdit.RemoveItem(_stash, guid))
            {
                StatusMessage = "That item is no longer present.";
                return;
            }

            var result = await _files.SaveStashAsync(_saveFolder, _stash);
            StatusMessage = result.Ok
                ? "Removed the item from MetaInventory.json — a backup was taken."
                : "Apply failed; the original MetaInventory.json is unchanged.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Remove failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }

        await LoadAsync();
    }
}
