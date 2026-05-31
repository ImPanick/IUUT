using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;
using IUUT.Core.Models;
using IUUT.Core.Services;

namespace IUUT.App.ViewModels;

/// <summary>
/// One character in the Characters &amp; Talents editor (master §11.5). Holds the user's desired
/// values (name, XP, debt, dead/abandoned, per-talent rank) seeded from the model; nothing touches
/// the model until <see cref="WriteTo"/> reconciles every field through <see cref="CharacterEditService"/>
/// at apply time.
/// </summary>
public sealed class CharacterSlotViewModel : ObservableObject
{
    private readonly GameCatalogs _catalogs;

    private string _name;
    private long _experience;
    private long _debt;
    private bool _isDead;
    private bool _isAbandoned;

    /// <summary>Wraps a character model for editing.</summary>
    public CharacterSlotViewModel(CharacterModel model, GameCatalogs catalogs)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(catalogs);
        Model = model;
        _catalogs = catalogs;

        _name = model.CharacterName;
        _experience = model.XP;
        _debt = model.XP_Debt;
        _isDead = model.IsDead;
        _isAbandoned = model.IsAbandoned;
        ChrSlot = model.ChrSlot;
        Location = string.IsNullOrEmpty(model.Location) ? "—" : model.Location;
        LastProspectId = string.IsNullOrEmpty(model.LastProspectId) ? "—" : model.LastProspectId;

        Talents = [];
        foreach (var talent in model.Talents.OrderBy(t => _catalogs.Talents.Label(t.RowName), StringComparer.OrdinalIgnoreCase))
        {
            Talents.Add(new TalentRowViewModel(talent.RowName, _catalogs.Talents.Label(talent.RowName), talent.Rank));
        }
    }

    /// <summary>The underlying model this wrapper edits (reconciled on apply).</summary>
    public CharacterModel Model { get; }

    /// <summary>The character's slot number (read-only identity).</summary>
    public int ChrSlot { get; }

    /// <summary>Last terrain key (read-only display).</summary>
    public string Location { get; }

    /// <summary>Last prospect id (read-only display).</summary>
    public string LastProspectId { get; }

    /// <summary>The character's editable talents.</summary>
    public ObservableCollection<TalentRowViewModel> Talents { get; }

    /// <summary>Editable display name.</summary>
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnPropertyChanged(nameof(DisplayLabel));
            }
        }
    }

    /// <summary>Editable total experience.</summary>
    public long Experience
    {
        get => _experience;
        set => SetProperty(ref _experience, value);
    }

    /// <summary>Editable XP debt pool (0 clears it).</summary>
    public long Debt
    {
        get => _debt;
        set => SetProperty(ref _debt, value);
    }

    /// <summary>Permadeath flag (uncheck to revive).</summary>
    public bool IsDead
    {
        get => _isDead;
        set => SetProperty(ref _isDead, value);
    }

    /// <summary>Abandoned-in-prospect flag.</summary>
    public bool IsAbandoned
    {
        get => _isAbandoned;
        set => SetProperty(ref _isAbandoned, value);
    }

    /// <summary>Label for the character selector.</summary>
    public string DisplayLabel => $"{(string.IsNullOrWhiteSpace(Name) ? "(unnamed)" : Name)}  ·  Slot {ChrSlot}";

    /// <summary>How many talents the character currently has (for the header).</summary>
    public int TalentCount => Talents.Count;

    /// <summary>Maxes this character's talents in the editor: every non-Reroute row → 4, plus the Genetics rows.</summary>
    public void MaxTalents()
    {
        foreach (var talent in Talents)
        {
            if (!talent.RowName.Contains("Reroute", StringComparison.Ordinal))
            {
                talent.Rank = CharacterEditService.MaxTalentRank;
            }
        }

        var present = new HashSet<string>(Talents.Select(t => t.RowName), StringComparer.Ordinal);
        foreach (var genetics in LazyMaxService.GeneticsTalents)
        {
            if (present.Add(genetics))
            {
                Talents.Add(new TalentRowViewModel(genetics, _catalogs.Talents.Label(genetics), CharacterEditService.MaxTalentRank));
            }
        }

        OnPropertyChanged(nameof(TalentCount));
    }

    /// <summary>Sets XP to a maxed value and clears debt (review, then apply).</summary>
    public void MaxExperience()
    {
        Experience = LazyMaxService.MinMaxedExperience;
        Debt = 0;
    }

    /// <summary>Reconciles all edits into the model through the edit service (called at apply time).</summary>
    public void WriteTo(CharacterEditService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        if (!string.IsNullOrWhiteSpace(Name))
        {
            service.Rename(Model, Name);
        }

        service.SetExperience(Model, Experience);
        service.SetDebt(Model, Debt);
        service.SetDead(Model, IsDead);
        service.SetAbandoned(Model, IsAbandoned);

        foreach (var talent in Talents)
        {
            service.SetTalentRank(Model, talent.RowName, talent.Rank);
        }
    }
}
