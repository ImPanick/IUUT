using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;
using IUUT.Core.Services;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Characters &amp; Talents editor (master §11.5): loads the selected save's roster, lets the
/// user edit one character's name, XP, debt, dead/abandoned flags, and per-talent rank (with
/// "Max talents" / "Max XP" helpers), then previews + applies through <see cref="CustomApplyService"/>
/// — backed up and atomic. The confirm dialog lives in the view; this view-model stays WPF-free.
/// </summary>
public sealed class CharacterEditorViewModel : ObservableObject
{
    private readonly CustomApplyService _apply;
    private readonly CharacterEditService _service;
    private readonly GameCatalogs _catalogs;
    private readonly string _saveFolder;

    private SaveEditBundle? _bundle;
    private CharacterSlotViewModel? _selectedCharacter;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the editor for one save profile folder.</summary>
    public CharacterEditorViewModel(
        CustomApplyService apply,
        CharacterEditService service,
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

        Characters = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        MaxTalentsCommand = new RelayCommand(MaxTalents, () => !IsBusy && SelectedCharacter is not null);
        MaxExperienceCommand = new RelayCommand(MaxExperience, () => !IsBusy && SelectedCharacter is not null);
    }

    /// <summary>The profile being edited (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The save's character roster.</summary>
    public ObservableCollection<CharacterSlotViewModel> Characters { get; }

    /// <summary>Reloads the save into the editor.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Maxes the selected character's talents (review, then Apply).</summary>
    public IRelayCommand MaxTalentsCommand { get; }

    /// <summary>Sets the selected character's XP to a maxed value and clears debt (review, then Apply).</summary>
    public IRelayCommand MaxExperienceCommand { get; }

    /// <summary>The character currently being edited.</summary>
    public CharacterSlotViewModel? SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            if (SetProperty(ref _selectedCharacter, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                MaxTalentsCommand.NotifyCanExecuteChanged();
                MaxExperienceCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Whether a character is selected (drives the detail panel's visibility).</summary>
    public bool HasSelection => SelectedCharacter is not null;

    /// <summary>True while loading or applying.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                MaxTalentsCommand.NotifyCanExecuteChanged();
                MaxExperienceCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Status-bar message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>True once the save's Characters.json parsed and the editor is usable.</summary>
    public bool IsLoaded => _bundle is not null;

    /// <summary>Loads (or reloads) the save's roster into the editor.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _bundle = await _apply.LoadAsync(_saveFolder);
            Characters.Clear();

            if (_bundle is null)
            {
                SelectedCharacter = null;
                StatusMessage = "Could not load this save's Characters.json (missing or unreadable).";
                return;
            }

            foreach (var character in _bundle.Characters.OrderBy(c => c.ChrSlot))
            {
                Characters.Add(new CharacterSlotViewModel(character, _catalogs));
            }

            SelectedCharacter = Characters.Count > 0 ? Characters[0] : null;
            StatusMessage = Characters.Count > 0
                ? $"Loaded {Characters.Count} character(s) for “{ProfileLabel}”."
                : "This save has no characters.";
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

    /// <summary>Applies the edited roster (call after a user confirm).</summary>
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
            foreach (var character in Characters)
            {
                character.WriteTo(_service);
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
                ? $"Applied character edits — {report.Message} A backup was taken."
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
        var keepSlot = SelectedCharacter?.ChrSlot;
        await LoadAsync();
        if (keepSlot is not null)
        {
            SelectedCharacter = Characters.FirstOrDefault(c => c.ChrSlot == keepSlot) ?? SelectedCharacter;
        }
    }

    private void MaxTalents()
    {
        if (SelectedCharacter is null)
        {
            return;
        }

        SelectedCharacter.MaxTalents();
        StatusMessage = $"Maxed talents for “{SelectedCharacter.Name}” — review, then Apply.";
    }

    private void MaxExperience()
    {
        if (SelectedCharacter is null)
        {
            return;
        }

        SelectedCharacter.MaxExperience();
        var xp = LazyMaxService.MinMaxedExperience.ToString("N0", CultureInfo.CurrentCulture);
        StatusMessage = $"Set XP to {xp} and cleared debt for “{SelectedCharacter.Name}” — review, then Apply.";
    }
}
