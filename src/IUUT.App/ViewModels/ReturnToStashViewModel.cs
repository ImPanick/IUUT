using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;

namespace IUUT.App.ViewModels;

/// <summary>One kind of trapped item shown in the Return-to-Stash preview.</summary>
public sealed record TrappedItemViewModel(string RowName, string Label, int SlotCount, int TotalQuantity);

/// <summary>
/// Return to Stash (Tier 2 RESCUE): lists the items trapped in a prospect's world save and
/// returns them all to the orbital stash through <see cref="ProspectReturnFileService"/> —
/// stash written before the prospect so a mid-operation failure duplicates (recoverable from
/// backup) rather than losing items. One-shot action; nothing is staged.
/// </summary>
public sealed class ReturnToStashViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly ProspectReturnFileService _return;
    private readonly GameCatalogs _catalogs;
    private readonly string _saveFolder;

    private NamedFileViewModel? _selectedProspect;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the panel for one save profile folder.</summary>
    public ReturnToStashViewModel(
        CustomFileService files,
        ProspectReturnFileService returnService,
        GameCatalogs catalogs,
        string saveFolder,
        string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(returnService);
        ArgumentNullException.ThrowIfNull(catalogs);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _files = files;
        _return = returnService;
        _catalogs = catalogs;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Prospects = [];
        TrappedItems = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    /// <summary>The profile being edited (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The save's prospect world files.</summary>
    public ObservableCollection<NamedFileViewModel> Prospects { get; }

    /// <summary>What is trapped in the selected prospect (read-only preview).</summary>
    public ObservableCollection<TrappedItemViewModel> TrappedItems { get; }

    /// <summary>Relists the prospect files.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>The prospect being previewed.</summary>
    public NamedFileViewModel? SelectedProspect
    {
        get => _selectedProspect;
        set
        {
            if (SetProperty(ref _selectedProspect, value))
            {
                _ = PreviewAsync();
            }
        }
    }

    /// <summary>Whether the selected prospect holds anything to return (enables the button).</summary>
    public bool HasTrapped => TrappedItems.Count > 0;

    /// <summary>Preview totals for the confirm dialog and header.</summary>
    public string Summary => HasTrapped
        ? $"{TrappedItems.Count:N0} item kind(s) · {TrappedItems.Sum(t => t.SlotCount):N0} slot(s) · {TrappedItems.Sum(t => t.TotalQuantity):N0} total quantity"
        : "Nothing trapped in this prospect.";

    /// <summary>True while loading, previewing, or returning.</summary>
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

    /// <summary>True once the prospect list was read and the panel is usable.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Lists (or relists) the prospect world files.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Prospects.Clear();
            TrappedItems.Clear();
            OnPropertyChanged(nameof(HasTrapped));

            foreach (var path in _files.ResolveProspectFiles(_saveFolder))
            {
                Prospects.Add(new NamedFileViewModel(path));
            }

            IsLoaded = true;
            if (Prospects.Count == 0)
            {
                StatusMessage = "No prospect world saves in this save folder.";
                return;
            }

            StatusMessage = $"Found {Prospects.Count} prospect(s) — pick one to preview its trapped items.";
            _selectedProspect = Prospects[0];
            OnPropertyChanged(nameof(SelectedProspect));
            await PreviewAsync();
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not list the prospects: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Returns every trapped item to the orbital stash (call after a user confirm).</summary>
    public async Task ReturnAllAsync()
    {
        if (IsBusy || SelectedProspect is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _return.ReturnAsync(SelectedProspect.Path, _saveFolder);
            if (!result.Ok)
            {
                StatusMessage = result.Error ?? "Return failed.";
                return;
            }

            StatusMessage = result.Moved is { SlotsRemoved: > 0 } moved
                ? $"Returned {moved.TotalQuantity:N0} item(s) from {moved.SlotsRemoved:N0} slot(s) to the stash (+{moved.StashStacksAdded:N0} stacks) — backups were taken."
                : "Nothing to return — the prospect holds no trapped items.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Return failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }

        // Re-preview so the panel reflects the now-empty prospect (keep the outcome visible).
        var outcome = StatusMessage;
        await PreviewAsync();
        StatusMessage = outcome;
    }

    private async Task PreviewAsync()
    {
        TrappedItems.Clear();

        if (SelectedProspect is null)
        {
            OnPropertyChanged(nameof(HasTrapped));
            OnPropertyChanged(nameof(Summary));
            return;
        }

        IsBusy = true;
        try
        {
            foreach (var item in await _return.PreviewAsync(SelectedProspect.Path))
            {
                TrappedItems.Add(new TrappedItemViewModel(
                    item.RowName,
                    _catalogs.Items.Label(item.RowName),
                    item.SlotCount,
                    item.TotalQuantity));
            }

            StatusMessage = HasTrapped
                ? $"{SelectedProspect.Name}: {Summary}"
                : $"{SelectedProspect.Name}: nothing trapped.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not preview the prospect: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            OnPropertyChanged(nameof(HasTrapped));
            OnPropertyChanged(nameof(Summary));
            IsBusy = false;
        }
    }
}
