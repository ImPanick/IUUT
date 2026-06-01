namespace IUUT.Core.Prospects.World;

/// <summary>What kind of actor owns an inventory slot, derived from its recorder
/// <c>ComponentClassName</c> (verified against real prospects).</summary>
public enum SlotOwnerKind
{
    /// <summary>The player character's carried inventory (<c>PlayerStateRecorderComponent</c>).</summary>
    PlayerCarried,

    /// <summary>A player-built/deployed container — crate, locker, base storage (<c>Deployable…</c>/<c>ContainerManager…</c>).</summary>
    DeployedStorage,

    /// <summary>A tamed mount's saddlebags (<c>IcarusMountCharacterRecorderComponent</c>).</summary>
    Mount,

    /// <summary>A machine's internal inventory — drill/extractor output, etc. (<c>Drill…</c>, geysers).</summary>
    Machine,

    /// <summary>Anything else / unrecognised recorder.</summary>
    Other,
}

/// <summary>
/// Classifies an inventory slot's owning actor from its recorder <c>ComponentClassName</c>, so
/// "return trapped items to the orbital stash" can be scoped — e.g. just the player's carried items, or
/// everything the player owns (carried + deployed storage + mount), versus world machines.
/// </summary>
public static class SlotOwner
{
    /// <summary>Maps a recorder component class to a <see cref="SlotOwnerKind"/>.</summary>
    public static SlotOwnerKind Classify(string? componentClass)
    {
        if (string.IsNullOrEmpty(componentClass))
        {
            return SlotOwnerKind.Other;
        }

        if (Has(componentClass, "PlayerState"))
        {
            return SlotOwnerKind.PlayerCarried;
        }

        if (Has(componentClass, "Mount"))
        {
            return SlotOwnerKind.Mount;
        }

        if (Has(componentClass, "Deployable") || Has(componentClass, "ContainerManager") || Has(componentClass, "Container"))
        {
            return SlotOwnerKind.DeployedStorage;
        }

        if (Has(componentClass, "Drill") || Has(componentClass, "Machine") || Has(componentClass, "Geyser") || Has(componentClass, "Processor"))
        {
            return SlotOwnerKind.Machine;
        }

        return SlotOwnerKind.Other;
    }

    /// <summary>
    /// Whether the slot belongs to the player's recoverable storage — carried inventory, deployed
    /// containers, or mount bags (everything a player would want back), excluding world machines.
    /// </summary>
    public static bool IsPlayerOwned(string? componentClass) =>
        Classify(componentClass) is SlotOwnerKind.PlayerCarried or SlotOwnerKind.DeployedStorage or SlotOwnerKind.Mount;

    private static bool Has(string value, string token) => value.Contains(token, StringComparison.OrdinalIgnoreCase);
}
