using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Loadouts viewer + recovery (master §8.7, Tier 2): shows each per-prospect loadout by the
/// prospect it is for and the gear it carries, and offers the two community rescue edits made
/// safe — INSURE ALL (the <c>bInsured</c> flip for gear stuck with an offline host) and RESTORE
/// MISSING (recreate dangling stash references with their exact GUID + RowName).
/// </summary>
public sealed class LoadoutsViewerViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly LoadoutCrossReference _crossReference;
    private readonly LoadoutRecoveryService _recovery;
    private readonly GameCatalogs _catalogs;
    private readonly string _saveFolder;

    private Core.Models.LoadoutsModel? _model;
    private Core.Models.MetaInventoryModel? _stash;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the viewer for one save profile folder.</summary>
    public LoadoutsViewerViewModel(
        CustomFileService files,
        LoadoutCrossReference crossReference,
        LoadoutRecoveryService recovery,
        GameCatalogs catalogs,
        string saveFolder,
        string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(crossReference);
        ArgumentNullException.ThrowIfNull(recovery);
        ArgumentNullException.ThrowIfNull(catalogs);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _files = files;
        _crossReference = crossReference;
        _recovery = recovery;
        _catalogs = catalogs;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Loadouts = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    /// <summary>The profile being viewed (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The per-prospect loadouts.</summary>
    public ObservableCollection<LoadoutRowViewModel> Loadouts { get; }

    /// <summary>Reloads the save into the viewer.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Header summary (loadout / prospect / item counts).</summary>
    public string Summary { get; private set; } = "";

    /// <summary>True while loading.</summary>
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

    /// <summary>True once the loadouts file parsed and the viewer is usable.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Loadouts not yet insured (drives the INSURE ALL button).</summary>
    public int UninsuredCount { get; private set; }

    /// <summary>Dangling stash references that can be recreated (drives RESTORE MISSING).</summary>
    public int RestorableCount { get; private set; }

    /// <summary>Whether INSURE ALL has anything to do.</summary>
    public bool CanInsure => UninsuredCount > 0 && !IsBusy;

    /// <summary>Whether RESTORE MISSING has anything to do (needs a readable stash).</summary>
    public bool CanRestore => RestorableCount > 0 && _stash is not null && !IsBusy;

    /// <summary>Flips <c>bInsured</c> on every uninsured loadout and writes Loadouts.json (call after a user confirm).</summary>
    public async Task InsureAllAsync()
    {
        if (IsBusy || _model is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var changed = _recovery.InsureAll(_model);
            if (changed == 0)
            {
                StatusMessage = "Every loadout is already insured.";
                return;
            }

            var result = await _files.SaveLoadoutsAsync(_saveFolder, _model);
            StatusMessage = result.Ok
                ? $"Insured {changed} loadout(s) — a backup of Loadouts.json was taken."
                : $"Insure failed; the original Loadouts.json is unchanged. {result.Error?.Message}";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Insure failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }

        var outcome = StatusMessage;
        await LoadAsync();
        if (IsLoaded)
        {
            StatusMessage = outcome;
        }
    }

    /// <summary>Recreates the dangling stash items (exact GUID + RowName) and writes MetaInventory.json (call after a user confirm).</summary>
    public async Task RestoreMissingAsync()
    {
        if (IsBusy || _model is null || _stash is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var added = _recovery.RestoreDangling(_model, _stash);
            if (added == 0)
            {
                StatusMessage = "Nothing restorable — no dangling references carry an item row.";
                return;
            }

            var result = await _files.SaveStashAsync(_saveFolder, _stash);
            StatusMessage = result.Ok
                ? $"Restored {added} missing item(s) to the stash — a backup of MetaInventory.json was taken."
                : $"Restore failed; the original MetaInventory.json is unchanged. {result.Error?.Message}";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }

        var outcome = StatusMessage;
        await LoadAsync();
        if (IsLoaded)
        {
            StatusMessage = outcome;
        }
    }

    /// <summary>Loads (or reloads) the loadouts + cross-reference into the viewer.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Loadouts.Clear();

            var loadouts = await _files.LoadLoadoutsAsync(_saveFolder);
            _model = loadouts;
            IsLoaded = loadouts is not null;
            if (loadouts is null)
            {
                Summary = "";
                OnPropertyChanged(nameof(Summary));
                StatusMessage = "Could not load this save's Loadout\\Loadouts.json (missing or unreadable).";
                return;
            }

            // Recovery preview needs the stash; a missing stash only disables RESTORE.
            _stash = await _files.LoadStashAsync(_saveFolder);
            var preview = _recovery.Preview(loadouts, _stash ?? new Core.Models.MetaInventoryModel());
            UninsuredCount = preview.UninsuredLoadouts;
            RestorableCount = preview.Restorable;
            OnPropertyChanged(nameof(UninsuredCount));
            OnPropertyChanged(nameof(RestorableCount));

            foreach (var summary in _crossReference.Summarize(loadouts)
                         .OrderBy(s => s.ProspectId, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(s => s.ChrSlot))
            {
                Loadouts.Add(new LoadoutRowViewModel(summary, _catalogs));
            }

            var prospectCount = Loadouts.Select(l => l.ProspectId).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var itemCount = Loadouts.Sum(l => l.ItemCount);
            Summary = $"{Loadouts.Count:N0} loadouts · {prospectCount:N0} prospects · {itemCount:N0} items configured";
            OnPropertyChanged(nameof(Summary));
            StatusMessage = $"Loaded loadouts for “{ProfileLabel}”.";
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
            OnPropertyChanged(nameof(CanInsure));
            OnPropertyChanged(nameof(CanRestore));
        }
    }
}
