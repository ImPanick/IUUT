using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;

namespace IUUT.App.ViewModels;

/// <summary>
/// The read-only Loadouts viewer (master §8.7): shows each per-prospect loadout by the prospect it
/// is for and the gear it carries — envirosuit + meta items resolved to friendly catalog names.
/// Loads <c>Loadout\Loadouts.json</c> via <see cref="CustomFileService"/>.
/// </summary>
public sealed class LoadoutsViewerViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly LoadoutCrossReference _crossReference;
    private readonly GameCatalogs _catalogs;
    private readonly string _saveFolder;

    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the viewer for one save profile folder.</summary>
    public LoadoutsViewerViewModel(
        CustomFileService files,
        LoadoutCrossReference crossReference,
        GameCatalogs catalogs,
        string saveFolder,
        string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(crossReference);
        ArgumentNullException.ThrowIfNull(catalogs);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _files = files;
        _crossReference = crossReference;
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

    /// <summary>Loads (or reloads) the loadouts + cross-reference into the viewer.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Loadouts.Clear();

            var loadouts = await _files.LoadLoadoutsAsync(_saveFolder);
            IsLoaded = loadouts is not null;
            if (loadouts is null)
            {
                Summary = "";
                OnPropertyChanged(nameof(Summary));
                StatusMessage = "Could not load this save's Loadout\\Loadouts.json (missing or unreadable).";
                return;
            }

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
        }
    }
}
