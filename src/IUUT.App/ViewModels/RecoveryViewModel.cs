using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Recovery;
using IUUT.Core.Services;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Broken Save Recovery page (master §10.1, §11.3): pick a save profile → Scan
/// (<see cref="RecoveryPlanner"/>) → review the per-file plan → Repair
/// (<see cref="RecoveryService"/>: master backup zip, then restore/template) → see the report +
/// advisories. Binding-shape only; the confirm dialog lives in the view.
/// </summary>
public sealed class RecoveryViewModel : ObservableObject
{
    private const string BackupsFolderName = "RecoveryBackups";

    private readonly HomeService _home;
    private readonly RecoveryPlanner _planner;
    private readonly RecoveryService _service;
    private readonly AppPaths _paths;

    private RecoveryPlan? _plan;
    private HomeSaveSlot? _selectedSlot;
    private bool _isBusy;
    private bool _partialRecovery;
    private string _statusMessage = "Pick a save profile, then Scan it for problems.";

    /// <summary>Creates the Recovery page over the Home, planner, executor, and app-paths services.</summary>
    public RecoveryViewModel(HomeService home, RecoveryPlanner planner, RecoveryService service, AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(home);
        ArgumentNullException.ThrowIfNull(planner);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(paths);
        _home = home;
        _planner = planner;
        _service = service;
        _paths = paths;

        Slots = [];
        ActionLines = [];
        Advisories = [];
        LoadSavesCommand = new AsyncRelayCommand(LoadSavesAsync);
        ScanCommand = new AsyncRelayCommand(ScanAsync, () => SelectedSlot is not null && !IsBusy);
    }

    /// <summary>Discovered save profiles to recover.</summary>
    public ObservableCollection<HomeSaveSlot> Slots { get; }

    /// <summary>One human-readable line per file the plan would touch.</summary>
    public ObservableCollection<string> ActionLines { get; }

    /// <summary>Post-repair advisories (Steam Cloud, conflicted copies, CFA, coherence).</summary>
    public ObservableCollection<string> Advisories { get; }

    /// <summary>(Re)lists save profiles.</summary>
    public IAsyncRelayCommand LoadSavesCommand { get; }

    /// <summary>Scans the selected profile and builds the recovery plan.</summary>
    public IAsyncRelayCommand ScanCommand { get; }

    /// <summary>The profile to recover.</summary>
    public HomeSaveSlot? SelectedSlot
    {
        get => _selectedSlot;
        set
        {
            if (SetProperty(ref _selectedSlot, value))
            {
                ResetPlan();
                ScanCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>True while a scan or repair is in flight.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ScanCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(CanRepair));
            }
        }
    }

    /// <summary>True when the plan can only partially recover (some files template-repaired / unrecoverable).</summary>
    public bool PartialRecovery
    {
        get => _partialRecovery;
        private set => SetProperty(ref _partialRecovery, value);
    }

    /// <summary>Status-bar message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Whether there is recoverable work to apply (drives the Repair button).</summary>
    public bool CanRepair => _plan is not null && _plan.HasWork && !IsBusy;

    private async Task LoadSavesAsync()
    {
        IsBusy = true;
        try
        {
            var state = await _home.LoadAsync(HomeService.DefaultSaveRoot);
            Slots.Clear();
            foreach (var slot in state.Slots)
            {
                Slots.Add(slot);
            }

            SelectedSlot = Slots.Count > 0 ? Slots[0] : null;
            StatusMessage = Slots.Count > 0 ? "Select a profile, then Scan." : "No save profiles found.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not list saves: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ScanAsync()
    {
        if (SelectedSlot is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await PopulatePlanAsync(SelectedSlot.FolderPath);
            StatusMessage = _plan is { HasWork: true }
                ? $"{ActionLines.Count} file(s) need recovery — review, then Repair."
                : "This save looks healthy — nothing to repair.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Applies the current plan (master backup → restore/template) and re-scans. Call after a user confirm.</summary>
    public async Task RepairAsync()
    {
        if (_plan is null || !_plan.HasWork)
        {
            return;
        }

        IsBusy = true;
        try
        {
            _paths.EnsureStateRoot();
            var backupDir = Path.Combine(_paths.StateRoot, BackupsFolderName);
            var report = await _service.ExecuteAsync(_plan, backupDir);

            Advisories.Clear();
            foreach (var advisory in report.Advisories)
            {
                Advisories.Add(advisory);
            }

            var zip = report.MasterBackupZipPath is null ? "(none)" : Path.GetFileName(report.MasterBackupZipPath);
            StatusMessage = report.Succeeded
                ? $"Recovered {report.ChangedCount} file(s). Full backup: {zip}."
                : $"Recovered {report.ChangedCount}; {report.FailedCount} could not be recovered. Full backup: {zip}.";

            await PopulatePlanAsync(SelectedSlot!.FolderPath); // refresh — files should now be healthy
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Repair failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PopulatePlanAsync(string folder)
    {
        ResetPlan();
        var plan = await Task.Run(() => _planner.Plan(folder));
        _plan = plan;

        foreach (var action in plan.Actions.Where(a => a.Outcome != RecoveryOutcome.AlreadyOk))
        {
            var note = string.IsNullOrEmpty(action.Note) ? "" : $"  ({action.Note})";
            ActionLines.Add($"{action.RelativePath}  →  {Describe(action.Outcome)}{note}");
        }

        if (ActionLines.Count == 0)
        {
            ActionLines.Add("All files parse cleanly — no recovery needed.");
        }

        PartialRecovery = plan.PartialRecovery;
        OnPropertyChanged(nameof(CanRepair));
    }

    private void ResetPlan()
    {
        _plan = null;
        ActionLines.Clear();
        PartialRecovery = false;
        OnPropertyChanged(nameof(CanRepair));
    }

    private static string Describe(RecoveryOutcome outcome) => outcome switch
    {
        RecoveryOutcome.RestoreFromGameBackup => "restore from game backup",
        RecoveryOutcome.RestoreFromIuutBackup => "restore from IUUT backup",
        RecoveryOutcome.TemplateRepair => "rebuild skeleton (data lost — partial)",
        RecoveryOutcome.Unrecoverable => "cannot recover automatically",
        _ => "ok",
    };
}
