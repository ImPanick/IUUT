using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Editing;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Account Flags editor (#81, master §8.5): a checklist of every <c>D_AccountFlags</c> unlock in
/// <c>Profile.json</c> <c>UnlockedFlags</c>, by friendly name via <see cref="AccountFlagEditService"/>.
/// Ids the profile has beyond the catalog stay visible and are never dropped (CONSTITUTION VI).
/// Previews + applies through <see cref="CustomApplyService"/> — backed up and atomic.
/// </summary>
public sealed class AccountFlagEditorViewModel : ObservableObject
{
    private readonly CustomApplyService _apply;
    private readonly AccountFlagEditService _service;
    private readonly string _saveFolder;

    private SaveEditBundle? _bundle;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the editor for one save profile folder.</summary>
    public AccountFlagEditorViewModel(
        CustomApplyService apply,
        AccountFlagEditService service,
        string saveFolder,
        string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _apply = apply;
        _service = service;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Flags = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        EnableAllCommand = new RelayCommand(EnableAll, () => !IsBusy && _bundle is not null);
    }

    /// <summary>The profile being edited (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>Every account flag with its checkbox state (staged until Apply).</summary>
    public ObservableCollection<AccountFlagRowViewModel> Flags { get; }

    /// <summary>Reloads the save into the editor.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Checks every flag (review, then Apply).</summary>
    public IRelayCommand EnableAllCommand { get; }

    /// <summary>How many flags are currently checked (header summary).</summary>
    public string Summary => $"{Flags.Count(f => f.IsEnabled):N0} of {Flags.Count:N0} account flags set";

    /// <summary>True while loading or applying.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                EnableAllCommand.NotifyCanExecuteChanged();
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

    /// <summary>Loads (or reloads) the profile's flags into the checklist.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _bundle = await _apply.LoadAsync(_saveFolder);
            Flags.Clear();

            if (_bundle is null)
            {
                StatusMessage = "Could not load this save's Profile.json (missing or unreadable).";
                return;
            }

            foreach (var state in _service.List(_bundle.Profile))
            {
                var row = new AccountFlagRowViewModel(state.Id, state.Label, state.Name, state.Enabled);
                // Keep the "N of M set" header live as individual checkboxes toggle (review finding).
                row.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(AccountFlagRowViewModel.IsEnabled))
                    {
                        OnPropertyChanged(nameof(Summary));
                    }
                };
                Flags.Add(row);
            }

            OnPropertyChanged(nameof(Summary));
            StatusMessage = $"Loaded {Flags.Count} account flags for “{ProfileLabel}”.";
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

    /// <summary>Applies the checklist to the profile (call after a user confirm).</summary>
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
            foreach (var row in Flags)
            {
                _service.SetById(_bundle.Profile, row.Id, row.IsEnabled);
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
                ? $"Applied account flags — {report.Message} A backup was taken."
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

    private void EnableAll()
    {
        foreach (var row in Flags)
        {
            row.IsEnabled = true;
        }

        OnPropertyChanged(nameof(Summary));
        StatusMessage = "Checked every account flag — review, then Apply.";
    }
}
