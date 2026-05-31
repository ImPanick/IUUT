using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;
using IUUT.Core.Services;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Custom editor shell (master doc §10.3): a save-profile selector + a category sidebar whose
/// selection swaps the <see cref="CurrentEditor"/> panel. Wired categories get their interactive
/// editor (e.g. Account &amp; Currencies); the rest show a placeholder until their UI lands (each
/// already has a tested Core service — see <see cref="CustomCategory.Status"/>).
/// </summary>
public sealed class CustomViewModel : ObservableObject
{
    private readonly HomeService _home;
    private readonly CustomApplyService _apply;
    private readonly CustomFileService _files;
    private readonly AccountEditService _account;
    private readonly CharacterEditService _character;
    private readonly AccoladeBestiaryEditService _accoladeBestiary;
    private readonly MountEditService _mount;
    private readonly StashEditService _stash;
    private readonly LoadoutCrossReference _loadoutCrossReference;
    private readonly GameCatalogs _catalogs;

    private HomeSaveSlot? _selectedSlot;
    private CustomCategory? _selectedCategory;
    private object? _currentEditor;
    private bool _isBusy;
    private string _statusMessage = "Pick a save profile, then choose a category.";

    /// <summary>Creates the Custom shell over the Home service + the edit pipeline.</summary>
    public CustomViewModel(
        HomeService home,
        CustomApplyService apply,
        CustomFileService files,
        AccountEditService account,
        CharacterEditService character,
        AccoladeBestiaryEditService accoladeBestiary,
        MountEditService mount,
        StashEditService stash,
        LoadoutCrossReference loadoutCrossReference,
        GameCatalogs catalogs)
    {
        ArgumentNullException.ThrowIfNull(home);
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(accoladeBestiary);
        ArgumentNullException.ThrowIfNull(mount);
        ArgumentNullException.ThrowIfNull(stash);
        ArgumentNullException.ThrowIfNull(loadoutCrossReference);
        ArgumentNullException.ThrowIfNull(catalogs);
        _home = home;
        _apply = apply;
        _files = files;
        _account = account;
        _character = character;
        _accoladeBestiary = accoladeBestiary;
        _mount = mount;
        _stash = stash;
        _loadoutCrossReference = loadoutCrossReference;
        _catalogs = catalogs;

        Slots = [];
        Categories = BuildCategories();
        _selectedCategory = Categories.Count > 0 ? Categories[0] : null;
        LoadSavesCommand = new AsyncRelayCommand(LoadSavesAsync);
        UpdateEditor();
    }

    /// <summary>Discovered save profiles.</summary>
    public ObservableCollection<HomeSaveSlot> Slots { get; }

    /// <summary>The editor categories shown in the sidebar.</summary>
    public IReadOnlyList<CustomCategory> Categories { get; }

    /// <summary>(Re)lists save profiles.</summary>
    public IAsyncRelayCommand LoadSavesCommand { get; }

    /// <summary>The save profile being edited.</summary>
    public HomeSaveSlot? SelectedSlot
    {
        get => _selectedSlot;
        set
        {
            if (SetProperty(ref _selectedSlot, value))
            {
                UpdateEditor();
            }
        }
    }

    /// <summary>The selected category (drives the editor panel).</summary>
    public CustomCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                UpdateEditor();
            }
        }
    }

    /// <summary>The editor for the selected category (swapped via implicit DataTemplate in the view).</summary>
    public object? CurrentEditor
    {
        get => _currentEditor;
        private set => SetProperty(ref _currentEditor, value);
    }

    /// <summary>True while the save list is loading.</summary>
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
            StatusMessage = Slots.Count > 0 ? "Select a category to edit." : "No save profiles found.";
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

    private void UpdateEditor()
    {
        var category = SelectedCategory;
        if (category is null)
        {
            CurrentEditor = null;
            return;
        }

        var slot = SelectedSlot;
        CurrentEditor = (category.Key, slot) switch
        {
            ("account", not null) =>
                new AccountEditorViewModel(_apply, _account, _catalogs, slot.FolderPath, slot.DisplayLabel),
            ("characters", not null) =>
                new CharacterEditorViewModel(_apply, _character, _catalogs, slot.FolderPath, slot.DisplayLabel),
            ("accolades", not null) =>
                new AccoladeBestiaryEditorViewModel(_apply, _accoladeBestiary, _catalogs, slot.FolderPath, slot.DisplayLabel),
            ("stash", not null) =>
                new StashViewerViewModel(_files, _stash, _loadoutCrossReference, _catalogs, slot.FolderPath, slot.DisplayLabel),
            ("loadouts", not null) =>
                new LoadoutsViewerViewModel(_files, _loadoutCrossReference, slot.FolderPath, slot.DisplayLabel),
            ("mounts", not null) =>
                new MountEditorViewModel(_files, _mount, slot.FolderPath, slot.DisplayLabel),
            _ => new PlaceholderEditorViewModel(category, needsProfile: slot is null),
        };
    }

    private static IReadOnlyList<CustomCategory> BuildCategories() =>
    [
        new()
        {
            Key = "account",
            Glyph = "💰",
            Label = "Account & Currencies",
            Description = "Orbital currencies and the workshop/prospect blueprint checklist.",
            Status = "Wired — AccountEditService.",
        },
        new()
        {
            Key = "characters",
            Glyph = "🧬",
            Label = "Characters & Talents",
            Description = "Per-character XP, debt, revive, rename, and per-talent rank (with a per-character max).",
            Status = "Core ready — CharacterEditService.",
        },
        new()
        {
            Key = "accolades",
            Glyph = "🏅",
            Label = "Accolades & Bestiary",
            Description = "Grant or remove accolades; set a creature group's scan points.",
            Status = "Core ready — AccoladeBestiaryEditService.",
        },
        new()
        {
            Key = "stash",
            Glyph = "📦",
            Label = "Orbital Stash",
            Description = "MetaInventory items: durability/stack, repair, replace, add, remove — with fresh GUIDs.",
            Status = "Core ready — StashEditService. Visual grid coming.",
        },
        new()
        {
            Key = "loadouts",
            Glyph = "🎒",
            Label = "Loadouts",
            Description = "Per-prospect loadouts; cross-reference item GUIDs with the stash.",
            Status = "Core ready — LoadoutCrossReference.",
        },
        new()
        {
            Key = "prospects",
            Glyph = "🗺",
            Label = "Prospects",
            Description = "Unstick a stuck character's prospect association; edit world headers (world blob preserved).",
            Status = "Core ready — ProspectEditService / ProspectBlobCodec.",
        },
        new()
        {
            Key = "mounts",
            Glyph = "🐎",
            Label = "Mounts",
            Description = "Mount name and level (the authoritative RecorderBlob is preserved).",
            Status = "Core ready — MountEditService.",
        },
        new()
        {
            Key = "flags",
            Glyph = "🚩",
            Label = "Engine Flags",
            Description = "The binary flags_*.dat engine unlock flag IDs.",
            Status = "Core ready — FlagsFileCodec.",
        },
        new()
        {
            Key = "raw",
            Glyph = "🧾",
            Label = "Advanced / Raw",
            Description = "Raw JSON viewer and export/import for any save file.",
            Status = "Coming.",
        },
    ];
}
