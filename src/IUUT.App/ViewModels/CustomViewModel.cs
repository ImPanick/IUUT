using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Services;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Custom editor shell (master doc §10.3): a save-profile selector + a category sidebar whose
/// selection swaps the editor panel. This is the navigation frame; the individual category editors
/// are wired to their Core services in follow-up steps (each has a tested service ready — see the
/// per-category <see cref="CustomCategory.Status"/>).
/// </summary>
public sealed class CustomViewModel : ObservableObject
{
    private readonly HomeService _home;

    private HomeSaveSlot? _selectedSlot;
    private CustomCategory? _selectedCategory;
    private bool _isBusy;
    private string _statusMessage = "Pick a save profile, then choose a category.";

    /// <summary>Creates the Custom shell over the Home service (for the save list).</summary>
    public CustomViewModel(HomeService home)
    {
        ArgumentNullException.ThrowIfNull(home);
        _home = home;

        Slots = [];
        Categories = BuildCategories();
        _selectedCategory = Categories.Count > 0 ? Categories[0] : null;
        LoadSavesCommand = new AsyncRelayCommand(LoadSavesAsync);
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
        set => SetProperty(ref _selectedSlot, value);
    }

    /// <summary>The selected category (drives the editor panel).</summary>
    public CustomCategory? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
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

    private static IReadOnlyList<CustomCategory> BuildCategories() =>
    [
        new()
        {
            Glyph = "💰",
            Label = "Account & Currencies",
            Description = "Orbital currencies, account unlock flags, and the workshop/prospect blueprint checklist.",
            Status = "Core ready — AccountEditService. Editor UI coming next.",
        },
        new()
        {
            Glyph = "🧬",
            Label = "Characters & Talents",
            Description = "Per-character XP, debt, revive, rename, and per-talent rank (with a per-character max).",
            Status = "Core ready — CharacterEditService.",
        },
        new()
        {
            Glyph = "🏅",
            Label = "Accolades & Bestiary",
            Description = "Grant or remove accolades; set a creature group's scan points.",
            Status = "Core ready — AccoladeBestiaryEditService.",
        },
        new()
        {
            Glyph = "📦",
            Label = "Orbital Stash",
            Description = "MetaInventory items: durability/stack, repair, replace, add, remove — with fresh GUIDs.",
            Status = "Core ready — StashEditService. Visual grid coming.",
        },
        new()
        {
            Glyph = "🎒",
            Label = "Loadouts",
            Description = "Per-prospect loadouts; cross-reference item GUIDs with the stash.",
            Status = "Core ready — LoadoutCrossReference.",
        },
        new()
        {
            Glyph = "🗺",
            Label = "Prospects",
            Description = "Unstick a stuck character's prospect association; edit world headers (world blob preserved).",
            Status = "Core ready — ProspectEditService / ProspectBlobCodec.",
        },
        new()
        {
            Glyph = "🐎",
            Label = "Mounts",
            Description = "Mount name and level (the authoritative RecorderBlob is preserved).",
            Status = "Core ready — MountEditService.",
        },
        new()
        {
            Glyph = "🚩",
            Label = "Engine Flags",
            Description = "The binary flags_*.dat engine unlock flag IDs.",
            Status = "Core ready — FlagsFileCodec.",
        },
        new()
        {
            Glyph = "⚙",
            Label = "Game Tuning",
            Description = "Engine.ini performance/visual cvar toggles — \"Buff FPS\", fog/volumetrics, quality scalars, FPS + net.",
            Status = "Future — Phase 7 (docs/GAME-TUNING.md). Bundled/sideloaded, offline; not yet built.",
        },
        new()
        {
            Glyph = "🧾",
            Label = "Advanced / Raw",
            Description = "Raw JSON viewer and export/import for any save file.",
            Status = "Coming.",
        },
    ];
}
