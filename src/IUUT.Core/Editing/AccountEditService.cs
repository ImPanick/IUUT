using IUUT.Core.Models;

namespace IUUT.Core.Editing;

/// <summary>
/// Custom-mode edits to <c>Profile.json</c> (master doc §11.6): account currencies, account
/// unlock flags, and the workshop/prospect blueprint checklist. Pure in-memory mutation of a
/// <see cref="ProfileModel"/>; persistence is the <see cref="CustomApplyService"/>'s job.
/// </summary>
public sealed class AccountEditService
{
    /// <summary>Rank for an unlocked <c>Workshop_*</c>/<c>Prospect_*</c> blueprint (always 1 — master §8.2).</summary>
    public const int WorkshopUnlockRank = 1;

    /// <summary>Sets a currency's amount, adding the entry if the account doesn't have it yet.</summary>
    public void SetCurrency(ProfileModel profile, string metaRow, long count)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrEmpty(metaRow);

        var existing = profile.MetaResources.FirstOrDefault(m => string.Equals(m.MetaRow, metaRow, StringComparison.Ordinal));
        if (existing is not null)
        {
            existing.Count = count;
        }
        else
        {
            profile.MetaResources.Add(new MetaResource { MetaRow = metaRow, Count = count });
        }
    }

    /// <summary>Adds an account unlock flag if absent; returns whether it was added.</summary>
    public bool AddFlag(ProfileModel profile, int flag)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.UnlockedFlags.Contains(flag))
        {
            return false;
        }

        profile.UnlockedFlags.Add(flag);
        return true;
    }

    /// <summary>Removes an account unlock flag; returns whether it was present.</summary>
    public bool RemoveFlag(ProfileModel profile, int flag)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return profile.UnlockedFlags.Remove(flag);
    }

    /// <summary>
    /// Toggles a workshop/prospect blueprint unlock: <paramref name="unlocked"/> = true ensures the
    /// row is present at rank 1; false removes it. Preserves any existing entry's extra fields.
    /// </summary>
    public void SetWorkshopUnlock(ProfileModel profile, string rowName, bool unlocked)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrEmpty(rowName);

        var existing = profile.Talents.FirstOrDefault(t => string.Equals(t.RowName, rowName, StringComparison.Ordinal));
        if (unlocked)
        {
            if (existing is not null)
            {
                existing.Rank = WorkshopUnlockRank;
            }
            else
            {
                profile.Talents.Add(new Talent { RowName = rowName, Rank = WorkshopUnlockRank });
            }
        }
        else if (existing is not null)
        {
            profile.Talents.Remove(existing);
        }
    }
}
