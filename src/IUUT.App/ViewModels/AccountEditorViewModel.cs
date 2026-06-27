using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;
using IUUT.Core.Models;
using IUUT.Core.Services;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Account &amp; Currencies editor (master §11.6): loads the selected save's <c>Profile.json</c>,
/// lets the user set each account currency and unlock every workshop/prospect blueprint, then
/// previews (diff + validate) and applies through <see cref="CustomApplyService"/> — backed up and
/// atomic. The confirm dialog lives in the view; this view-model stays WPF-free.
/// </summary>
public sealed class AccountEditorViewModel : ObservableObject
{
    /// <summary>The "max" amount the <see cref="MaxAllCurrenciesCommand"/> sets (shared with Lazy Max).</summary>
    public const long MaxCurrencyAmount = LazyMaxService.MaxedMetaResourceCount;

    private readonly CustomApplyService _apply;
    private readonly AccountEditService _account;
    private readonly GameCatalogs _catalogs;
    private readonly string _saveFolder;

    private SaveEditBundle? _bundle;
    private bool _isBusy;
    private int _blueprintsUnlocked;
    private int _blueprintsTotal;
    private bool _includeUnreleasedBlueprints;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the editor for one save profile folder.</summary>
    public AccountEditorViewModel(
        CustomApplyService apply,
        AccountEditService account,
        GameCatalogs catalogs,
        string saveFolder,
        string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(catalogs);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _apply = apply;
        _account = account;
        _catalogs = catalogs;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Currencies = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        MaxAllCurrenciesCommand = new RelayCommand(MaxAllCurrencies, () => !IsBusy && _bundle is not null);
    }

    /// <summary>The profile being edited (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The editable account currencies.</summary>
    public ObservableCollection<CurrencyRowViewModel> Currencies { get; }

    /// <summary>Reloads the save into the editor.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Sets every currency amount to <see cref="MaxCurrencyAmount"/> (review, then Apply).</summary>
    public IRelayCommand MaxAllCurrenciesCommand { get; }

    /// <summary>True while loading or applying.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                MaxAllCurrenciesCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>How many workshop/prospect blueprints are currently unlocked.</summary>
    public int BlueprintsUnlocked
    {
        get => _blueprintsUnlocked;
        private set
        {
            if (SetProperty(ref _blueprintsUnlocked, value))
            {
                OnPropertyChanged(nameof(BlueprintsSummary));
            }
        }
    }

    /// <summary>The total number of catalog workshop/prospect blueprints.</summary>
    public int BlueprintsTotal
    {
        get => _blueprintsTotal;
        private set
        {
            if (SetProperty(ref _blueprintsTotal, value))
            {
                OnPropertyChanged(nameof(BlueprintsSummary));
            }
        }
    }

    /// <summary>Human summary of blueprint unlock progress.</summary>
    public string BlueprintsSummary =>
        $"{BlueprintsUnlocked:N0} of {BlueprintsTotal:N0} workshop & prospect blueprints unlocked"
        + (IncludeUnreleasedBlueprints ? " · incl. unreleased" : "");

    /// <summary>
    /// When on, the blueprint count + "unlock all" include staged/not-live catalog blueprints
    /// (e.g. dev-gated content the game cooked out). Off by default so IUUT only grants what the
    /// live game recognises.
    /// </summary>
    public bool IncludeUnreleasedBlueprints
    {
        get => _includeUnreleasedBlueprints;
        set
        {
            if (SetProperty(ref _includeUnreleasedBlueprints, value))
            {
                OnPropertyChanged(nameof(BlueprintsSummary));
                if (_bundle is not null)
                {
                    RefreshBlueprintSummary(_bundle.Profile);
                }
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

    /// <summary>Loads (or reloads) the save's Profile.json into the editor.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _bundle = await _apply.LoadAsync(_saveFolder);
            Currencies.Clear();

            if (_bundle is null)
            {
                BlueprintsTotal = 0;
                BlueprintsUnlocked = 0;
                StatusMessage = "Could not load this save's Profile.json (missing or unreadable).";
                return;
            }

            var profile = _bundle.Profile;
            var seen = new HashSet<string>(StringComparer.Ordinal);

            // Known currencies first, in catalog order, seeded with the save's current amount.
            foreach (var row in _catalogs.MetaResources.Rows)
            {
                var existing = profile.MetaResources.FirstOrDefault(m => string.Equals(m.MetaRow, row.RowName, StringComparison.Ordinal));
                Currencies.Add(new CurrencyRowViewModel(row.RowName, row.Label, existing?.Count ?? 0));
                seen.Add(row.RowName);
            }

            // Then any currency present in the save that the catalog doesn't know about.
            foreach (var meta in profile.MetaResources)
            {
                if (seen.Add(meta.MetaRow))
                {
                    Currencies.Add(new CurrencyRowViewModel(meta.MetaRow, meta.MetaRow, meta.Count));
                }
            }

            RefreshBlueprintSummary(profile);
            StatusMessage = $"Loaded {Currencies.Count} currencies for “{ProfileLabel}”.";
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
            MaxAllCurrenciesCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Applies the edited currency amounts (call after a user confirm).</summary>
    public Task ApplyCurrenciesAsync() => ApplyAsync(
        profile =>
        {
            foreach (var row in Currencies)
            {
                _account.SetCurrency(profile, row.MetaRow, row.Count);
            }
        },
        "currency amounts");

    /// <summary>Unlocks every catalog workshop/prospect blueprint (call after a user confirm).</summary>
    public Task UnlockAllBlueprintsAsync() => ApplyAsync(
        profile =>
        {
            foreach (var rowName in BlueprintRowNames())
            {
                _account.SetWorkshopUnlock(profile, rowName, unlocked: true);
            }
        },
        "all workshop & prospect blueprints");

    private void MaxAllCurrencies()
    {
        foreach (var row in Currencies)
        {
            row.Count = MaxCurrencyAmount;
        }

        StatusMessage = $"Set all currencies to {MaxCurrencyAmount:N0} — review, then Apply.";
    }

    private async Task ApplyAsync(Action<ProfileModel> mutate, string what)
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
            mutate(_bundle.Profile);

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
                ? $"Applied {what} — {report.Message} A backup was taken."
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

        // Re-read from disk so the editor reflects what was actually written.
        await LoadAsync();
    }

    private void RefreshBlueprintSummary(ProfileModel profile)
    {
        var blueprintRows = BlueprintRowNames().ToList();
        BlueprintsTotal = blueprintRows.Count;

        var unlocked = new HashSet<string>(
            profile.Talents.Where(t => t.Rank >= AccountEditService.WorkshopUnlockRank).Select(t => t.RowName),
            StringComparer.Ordinal);
        BlueprintsUnlocked = blueprintRows.Count(unlocked.Contains);
    }

    private IEnumerable<string> BlueprintRowNames() =>
        (IncludeUnreleasedBlueprints ? _catalogs.Talents.RowNames : _catalogs.Talents.LiveRowNames)
            .Where(IsBlueprint);

    private static bool IsBlueprint(string rowName) =>
        rowName.StartsWith("Workshop_", StringComparison.Ordinal) ||
        rowName.StartsWith("Prospect_", StringComparison.Ordinal);
}
