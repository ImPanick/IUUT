namespace IUUT.Core.Services;

/// <summary>
/// What a <see cref="LazyMaxService"/> pass changed, per file (master doc §11.4 F-034:
/// the confirmation dialog lists the four touched files plus the character count). The
/// service mutates the in-memory models in place; these counts describe the result so
/// the UI can summarise before the user confirms and the change is written.
/// </summary>
public sealed record LazyMaxResult
{
    /// <summary>Number of character slots maxed (Characters.json).</summary>
    public int CharactersMaxed { get; init; }

    /// <summary>Talents applied to every character (the account-union size; rank 4 each, game clamps per row).</summary>
    public int TalentsPerCharacter { get; init; }

    /// <summary>Distinct account currencies raised to the maxed value (Profile.json).</summary>
    public int MetaResourcesMaxed { get; init; }

    /// <summary>Total <c>Workshop_*</c>/<c>Prospect_*</c> unlocks ensured at rank 1 (Profile.json).</summary>
    public int WorkshopUnlocksTotal { get; init; }

    /// <summary>How many of those workshop/prospect unlocks were newly added.</summary>
    public int WorkshopUnlocksAdded { get; init; }

    /// <summary>Accolades appended to <c>CompletedAccolades</c> (Accolades.json).</summary>
    public int AccoladesAdded { get; init; }

    /// <summary>Total creature groups at max <c>NumPoints</c> after the pass (BestiaryData.json).</summary>
    public int BestiaryGroupsTotal { get; init; }

    /// <summary>How many of those bestiary groups were newly added.</summary>
    public int BestiaryGroupsAdded { get; init; }

    /// <summary>Account mission/story flags newly set in <c>Profile.UnlockedFlags</c> (marks mission completion).</summary>
    public int MissionFlagsSet { get; init; }
}
