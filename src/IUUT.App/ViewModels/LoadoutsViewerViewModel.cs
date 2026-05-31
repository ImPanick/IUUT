using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Editing;

namespace IUUT.App.ViewModels;

/// <summary>
/// The read-only Loadouts viewer (master §8.7, §10.4): lists the per-prospect loadouts and surfaces
/// the loadout→stash GUID coupling — how many stash items the loadouts reference and which
/// references are <em>dangling</em> (referenced but missing from the stash, to restore together).
/// Loads <c>Loadout\Loadouts.json</c> + <c>MetaInventory.json</c> via <see cref="CustomFileService"/>.
/// </summary>
public sealed class LoadoutsViewerViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly LoadoutCrossReference _crossReference;
    private readonly string _saveFolder;

    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the viewer for one save profile folder.</summary>
    public LoadoutsViewerViewModel(
        CustomFileService files,
        LoadoutCrossReference crossReference,
        string saveFolder,
        string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(crossReference);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _files = files;
        _crossReference = crossReference;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Loadouts = [];
        DanglingReferences = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    /// <summary>The profile being viewed (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The per-prospect loadouts.</summary>
    public ObservableCollection<LoadoutRowViewModel> Loadouts { get; }

    /// <summary>Loadout-referenced item GUIDs that are not present in the stash.</summary>
    public ObservableCollection<string> DanglingReferences { get; }

    /// <summary>Reloads the save into the viewer.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Whether there are any dangling references (drives the warning panel).</summary>
    public bool HasDangling => DanglingReferences.Count > 0;

    /// <summary>Cross-reference summary.</summary>
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

    /// <summary>Loads (or reloads) the loadouts + cross-reference into the viewer.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Loadouts.Clear();
            DanglingReferences.Clear();

            var loadouts = await _files.LoadLoadoutsAsync(_saveFolder);
            IsLoaded = loadouts is not null;
            if (loadouts is null)
            {
                Summary = "";
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(HasDangling));
                StatusMessage = "Could not load this save's Loadout\\Loadouts.json (missing or unreadable).";
                return;
            }

            foreach (var entry in loadouts.Loadouts.OrderBy(l => l.ChrSlot))
            {
                Loadouts.Add(new LoadoutRowViewModel(entry.ChrSlot, entry.LoadoutGuid));
            }

            var referenced = _crossReference.ReferencedDatabaseGuids(loadouts);

            var stash = await _files.LoadStashAsync(_saveFolder);
            var danglingNote = "stash not loaded";
            if (stash is not null)
            {
                foreach (var guid in _crossReference.DanglingReferences(loadouts, stash))
                {
                    DanglingReferences.Add(guid);
                }

                danglingNote = $"{DanglingReferences.Count} dangling";
            }

            Summary = $"{Loadouts.Count:N0} loadouts · {referenced.Count:N0} item references · {danglingNote}";
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(HasDangling));
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
        }
    }
}
