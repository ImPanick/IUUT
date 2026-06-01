using IUUT.Core.Catalog;
using IUUT.Core.Models;

namespace IUUT.Core.Editing;

/// <summary>
/// Name-based editing of account unlock flags (<c>Profile.json</c> <c>UnlockedFlags</c>, backed by
/// <c>D_AccountFlags</c>). Resolves flag names ↔ ids via the <see cref="FlagCatalog"/> so the UI works in
/// human terms, and surfaces the full flag list with enabled state (including any enabled ids beyond the
/// catalog snapshot — those are preserved, never dropped). Pure in-memory mutation of a
/// <see cref="ProfileModel"/>; persistence is the apply layer's job.
/// </summary>
public sealed class AccountFlagEditService
{
    private readonly FlagCatalog _flags;

    /// <summary>Creates the service over the account-flag catalog.</summary>
    public AccountFlagEditService(FlagCatalog accountFlags)
    {
        ArgumentNullException.ThrowIfNull(accountFlags);
        _flags = accountFlags;
    }

    /// <summary>One account flag's id, RowName, human label, and whether the profile has it set.</summary>
    public sealed record AccountFlagState(int Id, string? Name, string Label, bool Enabled);

    /// <summary>
    /// Every account flag with its enabled state: all catalog flags first (by id), then any flag the
    /// profile has set that is beyond the catalog (labelled <c>Flag N</c>) so unknown-but-present ids stay
    /// visible and are never lost.
    /// </summary>
    public IReadOnlyList<AccountFlagState> List(ProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var enabled = new HashSet<int>(profile.UnlockedFlags);
        var result = new List<AccountFlagState>();
        var seen = new HashSet<int>();
        foreach (var id in _flags.Ids)
        {
            seen.Add(id);
            result.Add(new AccountFlagState(id, _flags.Name(id), _flags.Label(id), enabled.Contains(id)));
        }

        foreach (var id in profile.UnlockedFlags.Where(id => !seen.Contains(id)).Distinct().OrderBy(i => i))
        {
            result.Add(new AccountFlagState(id, null, _flags.Label(id), true));
        }

        return result;
    }

    /// <summary>Sets or clears a flag by its <c>D_AccountFlags</c> RowName. Returns false for an unknown name.</summary>
    public bool SetByName(ProfileModel profile, string flagName, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrEmpty(flagName);
        return _flags.TryGetId(flagName, out var id) && SetById(profile, id, enabled);
    }

    /// <summary>Sets or clears a flag by id. Returns whether the profile's flags actually changed.</summary>
    public bool SetById(ProfileModel profile, int id, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (enabled)
        {
            if (profile.UnlockedFlags.Contains(id))
            {
                return false;
            }

            profile.UnlockedFlags.Add(id);
            return true;
        }

        return profile.UnlockedFlags.Remove(id);
    }
}
