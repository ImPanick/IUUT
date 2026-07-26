using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Io;

namespace IUUT.App.ViewModels;

/// <summary>One backup in the Backup Manager list.</summary>
public sealed class BackupRowViewModel
{
    /// <summary>Creates the row over an inventory entry.</summary>
    public BackupRowViewModel(BackupInventoryService.BackupEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
    }

    /// <summary>The underlying inventory entry (the restore target).</summary>
    public BackupInventoryService.BackupEntry Entry { get; }

    /// <summary>The original file this backs up.</summary>
    public string OriginalName => Entry.OriginalName;

    /// <summary>When the backup was taken (local time) + size.</summary>
    public string Meta =>
        $"{Entry.TakenUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture)} · {Entry.SizeBytes / 1024.0:N0} KB";
}

/// <summary>
/// The Backup Manager (Tier 2 RESCUE): browse every IUUT timestamped backup in the save
/// folder, restore one (the current file is backed up first — a restore is itself
/// reversible), and prune old backups keeping the newest per file.
/// </summary>
public sealed class BackupManagerViewModel : ObservableObject
{
    /// <summary>How many backups per file a prune keeps.</summary>
    public const int PruneKeepPerFile = 3;

    private readonly BackupInventoryService _service;
    private readonly string _saveFolder;

    private BackupRowViewModel? _selectedBackup;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the manager for one save profile folder.</summary>
    public BackupManagerViewModel(BackupInventoryService service, string saveFolder, string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _service = service;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Backups = [];
        BackupsView = new Services.FilteredView<BackupRowViewModel>(
            Backups,
            static (b, s) => b.OriginalName.Contains(s, StringComparison.OrdinalIgnoreCase)
                          || b.Entry.BackupPath.Contains(s, StringComparison.OrdinalIgnoreCase));
        LoadCommand = new RelayCommand(Load);
    }

    /// <summary>The profile being managed (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>Every IUUT backup in the save folder, newest first.</summary>
    public ObservableCollection<BackupRowViewModel> Backups { get; }

    /// <summary>Searchable projection of <see cref="Backups"/>.</summary>
    public Services.FilteredView<BackupRowViewModel> BackupsView { get; }

    /// <summary>Relists the backups.</summary>
    public IRelayCommand LoadCommand { get; }

    /// <summary>The backup picked for restore.</summary>
    public BackupRowViewModel? SelectedBackup
    {
        get => _selectedBackup;
        set
        {
            if (SetProperty(ref _selectedBackup, value))
            {
                OnPropertyChanged(nameof(HasSelection));
            }
        }
    }

    /// <summary>Whether a backup is selected (enables Restore).</summary>
    public bool HasSelection => SelectedBackup is not null;

    /// <summary>Inventory summary for the header.</summary>
    public string Summary => Backups.Count == 0
        ? "No IUUT backups in this save folder yet."
        : $"{Backups.Count:N0} backup(s) · {Backups.Sum(b => b.Entry.SizeBytes) / (1024.0 * 1024.0):N1} MB · newest first";

    /// <summary>True while restoring or pruning.</summary>
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

    /// <summary>True once the inventory was read.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Restores the selected backup (call after a user confirm).</summary>
    public void RestoreSelected()
    {
        if (IsBusy || SelectedBackup is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = _service.Restore(SelectedBackup.Entry);
            StatusMessage = result.Ok
                ? $"Restored {SelectedBackup.OriginalName} — the replaced file was backed up first."
                : $"Restore failed: {result.Error}";
        }
        finally
        {
            IsBusy = false;
        }

        var outcome = StatusMessage;
        Load();
        StatusMessage = outcome;
    }

    /// <summary>Prunes old backups, keeping the newest <see cref="PruneKeepPerFile"/> per file (call after a user confirm).</summary>
    public void Prune()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var deleted = BackupInventoryService.Prune(_saveFolder, PruneKeepPerFile);
            StatusMessage = deleted == 0
                ? $"Nothing to prune — every file already has {PruneKeepPerFile} or fewer backups."
                : $"Pruned {deleted:N0} old backup(s), keeping the newest {PruneKeepPerFile} per file.";
        }
        finally
        {
            IsBusy = false;
        }

        var outcome = StatusMessage;
        Load();
        StatusMessage = outcome;
    }

    private void Load()
    {
        IsBusy = true;
        try
        {
            Backups.Clear();
            foreach (var entry in BackupInventoryService.List(_saveFolder))
            {
                Backups.Add(new BackupRowViewModel(entry));
            }

            IsLoaded = true;
            OnPropertyChanged(nameof(Summary));
            StatusMessage = Backups.Count == 0
                ? "No IUUT backups here yet — they appear as soon as an editor writes."
                : $"Listed {Backups.Count:N0} backup(s) for “{ProfileLabel}”.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not list the backups: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }
}
