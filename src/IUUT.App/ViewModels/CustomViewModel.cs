using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;
using IUUT.Core.Services;
using Wpf.Ui.Controls;

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
    private readonly FlagsEditService _flags;
    private readonly AccountFlagEditService _accountFlags;
    private readonly ProspectEditService _prospect;
    private readonly MissionCompletionService _missions;
    private readonly ProspectReturnFileService _prospectReturn;
    private readonly IUUT.Core.Io.BackupInventoryService _backups;
    private readonly LoadoutRecoveryService _loadoutRecovery;
    private readonly GameCatalogs _catalogs;
    private readonly Services.SaveRootState _saveRootState;
    private int _loadedRootVersion = -1;
    private bool _suppressDirtyGuard;

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
        FlagsEditService flags,
        AccountFlagEditService accountFlags,
        ProspectEditService prospect,
        MissionCompletionService missions,
        ProspectReturnFileService prospectReturn,
        IUUT.Core.Io.BackupInventoryService backups,
        LoadoutRecoveryService loadoutRecovery,
        GameCatalogs catalogs,
        Services.SaveRootState saveRootState)
    {
        ArgumentNullException.ThrowIfNull(loadoutRecovery);
        _loadoutRecovery = loadoutRecovery;
        ArgumentNullException.ThrowIfNull(saveRootState);
        ArgumentNullException.ThrowIfNull(accountFlags);
        ArgumentNullException.ThrowIfNull(missions);
        ArgumentNullException.ThrowIfNull(prospectReturn);
        ArgumentNullException.ThrowIfNull(backups);
        _missions = missions;
        _prospectReturn = prospectReturn;
        _backups = backups;
        _saveRootState = saveRootState;
        _accountFlags = accountFlags;
        ArgumentNullException.ThrowIfNull(home);
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(accoladeBestiary);
        ArgumentNullException.ThrowIfNull(mount);
        ArgumentNullException.ThrowIfNull(stash);
        ArgumentNullException.ThrowIfNull(loadoutCrossReference);
        ArgumentNullException.ThrowIfNull(flags);
        ArgumentNullException.ThrowIfNull(prospect);
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
        _flags = flags;
        _prospect = prospect;
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

    /// <summary>
    /// Asks the user to confirm discarding unapplied edits (wired by the view to a dialog).
    /// When unset, switches proceed without asking.
    /// </summary>
    public Func<string, bool>? ConfirmDiscard { get; set; }

    /// <summary>The save profile being edited.</summary>
    public HomeSaveSlot? SelectedSlot
    {
        get => _selectedSlot;
        set
        {
            if (Equals(_selectedSlot, value))
            {
                return;
            }

            if (!ConfirmSwitchAway())
            {
                SnapBack(nameof(SelectedSlot));
                return;
            }

            _selectedSlot = value;
            OnPropertyChanged();
            UpdateEditor();
        }
    }

    /// <summary>The selected category (drives the editor panel).</summary>
    public CustomCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (Equals(_selectedCategory, value))
            {
                return;
            }

            if (!ConfirmSwitchAway())
            {
                SnapBack(nameof(SelectedCategory));
                return;
            }

            _selectedCategory = value;
            OnPropertyChanged();
            UpdateEditor();
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

    /// <summary>True when the shared save root changed since this page last loaded its slot list
    /// (the view's Loaded handler reloads on navigation when stale — singleton VMs otherwise only
    /// auto-load once; review finding).</summary>
    public bool IsSaveRootStale => _loadedRootVersion != _saveRootState.Version;

    private async Task LoadSavesAsync()
    {
        // One up-front confirm covers the whole reload (which re-selects a slot below).
        if (!ConfirmSwitchAway())
        {
            return;
        }

        IsBusy = true;
        _suppressDirtyGuard = true;
        try
        {
            // The shared root browsed on Home — not the hardcoded default (elevation audit bug fix).
            _loadedRootVersion = _saveRootState.Version;
            var state = await _home.LoadAsync(_saveRootState.Current);
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
            _suppressDirtyGuard = false;
            IsBusy = false;
        }
    }

    // The Tier 1 dirty guard: an editor with staged edits must not be silently replaced.
    private bool ConfirmSwitchAway() =>
        _suppressDirtyGuard
        || CurrentEditor is not Services.IDirtyEditor { IsDirty: true }
        || ConfirmDiscard is null
        || ConfirmDiscard("This editor has changes that were not applied. Discard them?");

    // Re-raise after the in-flight binding completes so the sidebar/selector snaps back.
    private void SnapBack(string propertyName) =>
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => OnPropertyChanged(propertyName));

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
                new LoadoutsViewerViewModel(_files, _loadoutCrossReference, _loadoutRecovery, _catalogs, slot.FolderPath, slot.DisplayLabel),
            ("mounts", not null) =>
                new MountEditorViewModel(_files, _mount, slot.FolderPath, slot.DisplayLabel),
            ("flags", not null) =>
                new FlagEditorViewModel(_files, _flags, _catalogs.CharacterFlags, slot.FolderPath, slot.DisplayLabel),
            ("accountflags", not null) =>
                new AccountFlagEditorViewModel(_apply, _accountFlags, slot.FolderPath, slot.DisplayLabel),
            ("prospects", not null) =>
                new ProspectsEditorViewModel(_files, _prospect, _catalogs, slot.FolderPath, slot.DisplayLabel),
            ("missions", not null) =>
                new MissionsEditorViewModel(_apply, _missions, _catalogs, slot.FolderPath, slot.DisplayLabel),
            ("returntostash", not null) =>
                new ReturnToStashViewModel(_files, _prospectReturn, _catalogs, slot.FolderPath, slot.DisplayLabel),
            ("backupmanager", not null) =>
                new BackupManagerViewModel(_backups, slot.FolderPath, slot.DisplayLabel),
            ("prospectquests", not null) =>
                new ProspectQuestsViewModel(_files, slot.FolderPath, slot.DisplayLabel),
            ("raw", not null) =>
                new RawEditorViewModel(_files, slot.FolderPath, slot.DisplayLabel),
            _ => new PlaceholderEditorViewModel(category, needsProfile: slot is null),
        };
    }

    // DE-3 IA: intent groups (Progression / World / Rescue / Advanced) with Tier-2 homes
    // pre-placed as disabled entries, so upcoming features land in an obvious place.
    private static IReadOnlyList<CustomCategory> BuildCategories() =>
    [
        new()
        {
            Key = "account",
            Group = "PROGRESSION",
            Glyph = SymbolRegular.WalletCreditCard24,
            Label = "Account & Currencies",
            Description = "Orbital currencies and the workshop/prospect blueprint checklist.",
            Status = "Wired — AccountEditService.",
        },
        new()
        {
            Key = "characters",
            Group = "PROGRESSION",
            Glyph = SymbolRegular.Person24,
            Label = "Characters & Talents",
            Description = "Per-character XP, debt, revive, rename, and per-talent rank (with a per-character max).",
            Status = "Wired — CharacterEditService.",
        },
        new()
        {
            Key = "accolades",
            Group = "PROGRESSION",
            Glyph = SymbolRegular.Trophy24,
            Label = "Accolades & Bestiary",
            Description = "Grant or remove accolades; set a creature group's scan points.",
            Status = "Wired — AccoladeBestiaryEditService.",
        },
        new()
        {
            Key = "accountflags",
            Group = "PROGRESSION",
            Glyph = SymbolRegular.CheckboxChecked24,
            Label = "Account Flags",
            Description = "Profile.json UnlockedFlags — map/talent-grant unlocks as a named checklist.",
            Status = "Wired — AccountFlagEditService.",
        },
        new()
        {
            Key = "flags",
            Group = "PROGRESSION",
            Glyph = SymbolRegular.Flag24,
            Label = "Engine Flags",
            Description = "The binary flags_*.dat engine unlock flag IDs.",
            Status = "Wired — FlagsFileCodec.",
        },
        new()
        {
            Key = "missions",
            Group = "PROGRESSION",
            Glyph = SymbolRegular.Flag24,
            Label = "Missions",
            Description = "Mission checklist — completing a mission also completes its prerequisites.",
            Status = "Wired — MissionCompletionService.",
        },
        new()
        {
            Key = "stash",
            Group = "WORLD",
            Glyph = SymbolRegular.Box24,
            Label = "Orbital Stash",
            Description = "MetaInventory items: durability/stack, repair, replace, add, remove — with fresh GUIDs.",
            Status = "Wired — StashEditService + visual grid.",
        },
        new()
        {
            Key = "loadouts",
            Group = "WORLD",
            Glyph = SymbolRegular.Backpack24,
            Label = "Loadouts",
            Description = "Per-prospect loadouts; cross-reference item GUIDs with the stash.",
            Status = "Wired — LoadoutCrossReference.",
        },
        new()
        {
            Key = "prospects",
            Group = "WORLD",
            Glyph = SymbolRegular.Map24,
            Label = "Prospects",
            Description = "Unstick a stuck character's prospect association (world blob preserved).",
            Status = "Wired — ProspectEditService (header editing is roadmap Tier 2).",
        },
        new()
        {
            Key = "prospectquests",
            Group = "WORLD",
            Glyph = SymbolRegular.Flag24,
            Label = "Prospect Quests",
            Description = "Mission state inside each prospect's world save — reset a mission to replay it.",
            Status = "Wired — ProspectQuestReader/Editor.",
        },
        new()
        {
            Key = "mounts",
            Group = "WORLD",
            Glyph = SymbolRegular.AnimalPawPrint24,
            Label = "Mounts",
            Description = "Mount name and level (the authoritative RecorderBlob is preserved).",
            Status = "Wired — MountEditService.",
        },
        new()
        {
            Key = "returntostash",
            Group = "RESCUE",
            Glyph = SymbolRegular.Box24,
            Label = "Return to Stash",
            Description = "Recover items trapped in a prospect's world save back to the orbital stash.",
            Status = "Wired — ProspectReturnFileService.",
        },
        new()
        {
            Key = "backupmanager",
            Group = "RESCUE",
            Glyph = SymbolRegular.Archive24,
            Label = "Backup Manager",
            Description = "Browse, restore, and prune IUUT's timestamped backups.",
            Status = "Wired — BackupInventoryService.",
        },
        new()
        {
            Key = "raw",
            Group = "ADVANCED",
            Glyph = SymbolRegular.Code24,
            Label = "Advanced / Raw",
            Description = "Raw JSON viewer and export/import for any save file.",
            Status = "Wired — read-only viewer + validated import/export.",
        },
    ];
}
