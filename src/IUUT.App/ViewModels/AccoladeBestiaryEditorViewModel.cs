using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;
using IUUT.Core.Services;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Accolades &amp; Bestiary editor (master §11.7): a catalog-driven accolade checklist (grant /
/// revoke) and creature-group scan points, edited in memory then previewed + applied through
/// <see cref="CustomApplyService"/> — backed up and atomic. The confirm dialog lives in the view.
/// </summary>
public sealed class AccoladeBestiaryEditorViewModel : ObservableObject
{
    /// <summary>The "max" scan points the <see cref="MaxAllBestiaryCommand"/> sets (shared with Lazy Max).</summary>
    public const long MaxBestiaryPoints = LazyMaxService.MaxedBestiaryPoints;

    private readonly CustomApplyService _apply;
    private readonly AccoladeBestiaryEditService _service;
    private readonly GameCatalogs _catalogs;
    private readonly string _saveFolder;

    private SaveEditBundle? _bundle;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the editor for one save profile folder.</summary>
    public AccoladeBestiaryEditorViewModel(
        CustomApplyService apply,
        AccoladeBestiaryEditService service,
        GameCatalogs catalogs,
        string saveFolder,
        string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(catalogs);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _apply = apply;
        _service = service;
        _catalogs = catalogs;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Accolades = [];
        Bestiary = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        GrantAllAccoladesCommand = new RelayCommand(() => SetAllAccolades(true));
        RevokeAllAccoladesCommand = new RelayCommand(() => SetAllAccolades(false));
        MaxAllBestiaryCommand = new RelayCommand(MaxAllBestiary);
    }

    /// <summary>The profile being edited (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The catalog accolade checklist (+ any held accolade the catalog doesn't know).</summary>
    public ObservableCollection<AccoladeRowViewModel> Accolades { get; }

    /// <summary>The catalog creature groups (+ any tracked group the catalog doesn't know).</summary>
    public ObservableCollection<BestiaryRowViewModel> Bestiary { get; }

    /// <summary>Reloads the save into the editor.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Grants every listed accolade (review, then Apply).</summary>
    public IRelayCommand GrantAllAccoladesCommand { get; }

    /// <summary>Revokes every listed accolade (review, then Apply).</summary>
    public IRelayCommand RevokeAllAccoladesCommand { get; }

    /// <summary>Sets every creature group's points to <see cref="MaxBestiaryPoints"/> (review, then Apply).</summary>
    public IRelayCommand MaxAllBestiaryCommand { get; }

    /// <summary>Live "granted of total" accolade summary.</summary>
    public string AccoladesSummary => $"{Accolades.Count(a => a.IsGranted):N0} of {Accolades.Count:N0} granted";

    /// <summary>Creature-group count summary.</summary>
    public string BestiarySummary => $"{Bestiary.Count:N0} creature groups";

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

    /// <summary>True once the save's files parsed and the editor is usable.</summary>
    public bool IsLoaded => _bundle is not null;

    /// <summary>Loads (or reloads) the save into the editor.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _bundle = await _apply.LoadAsync(_saveFolder);
            ClearRows();

            if (_bundle is null)
            {
                StatusMessage = "Could not load this save's Accolades.json / BestiaryData.json (missing or unreadable).";
                return;
            }

            BuildAccoladeRows(_bundle);
            BuildBestiaryRows(_bundle);

            OnPropertyChanged(nameof(AccoladesSummary));
            OnPropertyChanged(nameof(BestiarySummary));
            StatusMessage = $"Loaded {Accolades.Count} accolades and {Bestiary.Count} creature groups for “{ProfileLabel}”.";
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

    /// <summary>Applies the edited accolades + bestiary (call after a user confirm).</summary>
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
            foreach (var accolade in Accolades)
            {
                if (accolade.IsGranted)
                {
                    _service.AddAccolade(_bundle.Accolades, accolade.RowName);
                }
                else
                {
                    _service.RemoveAccolade(_bundle.Accolades, accolade.RowName);
                }
            }

            foreach (var group in Bestiary)
            {
                if (group.Points > 0)
                {
                    _service.SetBestiaryPoints(_bundle.Bestiary, group.RowName, group.Points);
                }
                else
                {
                    _service.RemoveBestiaryGroup(_bundle.Bestiary, group.RowName);
                }
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
                ? $"Applied accolades & bestiary — {report.Message} A backup was taken."
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

        await LoadAsync();
    }

    private void BuildAccoladeRows(SaveEditBundle bundle)
    {
        var granted = new HashSet<string>(
            bundle.Accolades.CompletedAccolades.Select(a => a.Accolade.RowName),
            StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in _catalogs.Accolades.Rows.OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase))
        {
            AddAccoladeRow(row.RowName, row.Label, granted.Contains(row.RowName));
            seen.Add(row.RowName);
        }

        foreach (var name in granted)
        {
            if (seen.Add(name))
            {
                AddAccoladeRow(name, name, isGranted: true);
            }
        }
    }

    private void BuildBestiaryRows(SaveEditBundle bundle)
    {
        var points = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var entry in bundle.Bestiary.BestiaryTracking)
        {
            points[entry.BestiaryGroup.RowName] = entry.NumPoints;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in _catalogs.Bestiary.Rows.OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase))
        {
            Bestiary.Add(new BestiaryRowViewModel(row.RowName, row.Label, points.GetValueOrDefault(row.RowName)));
            seen.Add(row.RowName);
        }

        foreach (var (name, value) in points)
        {
            if (seen.Add(name))
            {
                Bestiary.Add(new BestiaryRowViewModel(name, name, value));
            }
        }
    }

    private void AddAccoladeRow(string rowName, string label, bool isGranted)
    {
        var row = new AccoladeRowViewModel(rowName, label, isGranted);
        row.PropertyChanged += OnAccoladeChanged;
        Accolades.Add(row);
    }

    private void ClearRows()
    {
        foreach (var row in Accolades)
        {
            row.PropertyChanged -= OnAccoladeChanged;
        }

        Accolades.Clear();
        Bestiary.Clear();
    }

    private void OnAccoladeChanged(object? sender, PropertyChangedEventArgs e) =>
        OnPropertyChanged(nameof(AccoladesSummary));

    private void SetAllAccolades(bool granted)
    {
        foreach (var accolade in Accolades)
        {
            accolade.IsGranted = granted;
        }

        StatusMessage = granted
            ? "Granted all accolades — review, then Apply."
            : "Revoked all accolades — review, then Apply.";
    }

    private void MaxAllBestiary()
    {
        foreach (var group in Bestiary)
        {
            group.Points = MaxBestiaryPoints;
        }

        StatusMessage = $"Set all creature groups to {MaxBestiaryPoints:N0} points — review, then Apply.";
    }
}
