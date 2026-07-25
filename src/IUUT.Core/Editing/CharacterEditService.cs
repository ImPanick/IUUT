using IUUT.Core.Catalog;
using IUUT.Core.Models;
using IUUT.Core.Services;

namespace IUUT.Core.Editing;

/// <summary>
/// Custom-mode edits to a single <see cref="CharacterModel"/> (master doc §11.5): XP, debt,
/// dead/abandoned toggles, name, and per-talent rank, plus a per-character "max talents". Pure
/// in-memory mutation; persistence is the <see cref="CustomApplyService"/>'s job.
/// </summary>
public sealed class CharacterEditService
{
    /// <summary>Fallback max rank for talents without a mined <c>maxRank</c> (unlock-style/unknown
    /// rows); the game clamps each row to its true max on load regardless (§8.3).</summary>
    public const int MaxTalentRank = 4;

    /// <summary>Sets total experience.</summary>
    public void SetExperience(CharacterModel character, long xp)
    {
        ArgumentNullException.ThrowIfNull(character);
        character.XP = xp;
    }

    /// <summary>Sets the XP debt pool (0 clears it).</summary>
    public void SetDebt(CharacterModel character, long debt)
    {
        ArgumentNullException.ThrowIfNull(character);
        character.XP_Debt = debt;
    }

    /// <summary>Sets the permadeath flag (false revives).</summary>
    public void SetDead(CharacterModel character, bool dead)
    {
        ArgumentNullException.ThrowIfNull(character);
        character.IsDead = dead;
    }

    /// <summary>Sets the abandoned-in-prospect flag.</summary>
    public void SetAbandoned(CharacterModel character, bool abandoned)
    {
        ArgumentNullException.ThrowIfNull(character);
        character.IsAbandoned = abandoned;
    }

    /// <summary>Renames the character (name must be non-empty).</summary>
    public void Rename(CharacterModel character, string name)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentException.ThrowIfNullOrEmpty(name);
        character.CharacterName = name;
    }

    /// <summary>
    /// Sets a talent's rank: <paramref name="rank"/> ≤ 0 removes the row; otherwise the row is added
    /// or updated. Preserves an existing entry's extra fields.
    /// </summary>
    public void SetTalentRank(CharacterModel character, string rowName, int rank)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentException.ThrowIfNullOrEmpty(rowName);

        var existing = character.Talents.FirstOrDefault(t => string.Equals(t.RowName, rowName, StringComparison.Ordinal));
        if (rank <= 0)
        {
            if (existing is not null)
            {
                character.Talents.Remove(existing);
            }
        }
        else if (existing is not null)
        {
            existing.Rank = rank;
        }
        else
        {
            character.Talents.Add(new Talent { RowName = rowName, Rank = rank });
        }
    }

    /// <summary>
    /// Maxes this character's talents only (not XP/flags — that's Lazy Max): every existing
    /// non-<c>*Reroute*</c> talent → its true mined max from <paramref name="talents"/> (fallback
    /// <see cref="MaxTalentRank"/> when no catalog / unknown row), plus the 16 Genetics rows added
    /// at max. Returns the number of talents at their max afterwards.
    /// </summary>
    public int MaxTalents(CharacterModel character, CatalogTable? talents = null)
    {
        ArgumentNullException.ThrowIfNull(character);

        int MaxOf(string rowName) => talents?.MaxRank(rowName, MaxTalentRank) ?? MaxTalentRank;

        var present = new HashSet<string>(character.Talents.Select(t => t.RowName), StringComparer.Ordinal);
        foreach (var talent in character.Talents.Where(t => !IsReroute(t.RowName)))
        {
            // Raise-only: never regress a rank already above the (possibly stale) mined max.
            talent.Rank = Math.Max(talent.Rank, MaxOf(talent.RowName));
        }

        foreach (var genetics in LazyMaxService.GeneticsTalents)
        {
            if (present.Add(genetics))
            {
                character.Talents.Add(new Talent { RowName = genetics, Rank = MaxOf(genetics) });
            }
        }

        return character.Talents.Count(t => t.Rank == MaxOf(t.RowName));
    }

    private static bool IsReroute(string rowName) => rowName.Contains("Reroute", StringComparison.Ordinal);
}
