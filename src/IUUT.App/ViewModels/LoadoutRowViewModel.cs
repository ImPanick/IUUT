namespace IUUT.App.ViewModels;

/// <summary>One per-prospect loadout in the read-only Loadouts viewer (master §8.7).</summary>
public sealed class LoadoutRowViewModel
{
    /// <summary>Creates a loadout row.</summary>
    public LoadoutRowViewModel(int chrSlot, string loadoutGuid)
    {
        ChrSlot = chrSlot;
        LoadoutGuid = string.IsNullOrEmpty(loadoutGuid) ? "—" : loadoutGuid;
    }

    /// <summary>The character slot this loadout belongs to.</summary>
    public int ChrSlot { get; }

    /// <summary>The loadout-level id.</summary>
    public string LoadoutGuid { get; }

    /// <summary>Label for the list.</summary>
    public string SlotLabel => $"Slot {ChrSlot}";
}
