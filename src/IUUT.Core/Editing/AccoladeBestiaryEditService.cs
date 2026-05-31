using System.Globalization;
using IUUT.Core.Abstractions;
using IUUT.Core.Models;

namespace IUUT.Core.Editing;

/// <summary>
/// Custom-mode edits to <c>Accolades.json</c> and <c>BestiaryData.json</c> (master doc §11.7):
/// grant/remove an accolade and set/remove a creature group's scan points. Pure in-memory
/// mutation; persistence is the <see cref="CustomApplyService"/>'s job.
/// </summary>
public sealed class AccoladeBestiaryEditService
{
    private const string AccoladesDataTable = "D_Accolades";
    private const string BestiaryDataTable = "D_BestiaryData";

    private readonly IClock _clock;

    /// <summary>Creates the service with a clock (for accolade completion timestamps).</summary>
    public AccoladeBestiaryEditService(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    /// <summary>Grants an accolade if absent (timestamped now, empty ProspectID); returns whether it was added.</summary>
    public bool AddAccolade(AccoladesModel accolades, string rowName)
    {
        ArgumentNullException.ThrowIfNull(accolades);
        ArgumentException.ThrowIfNullOrEmpty(rowName);

        if (accolades.CompletedAccolades.Any(a => string.Equals(a.Accolade.RowName, rowName, StringComparison.Ordinal)))
        {
            return false;
        }

        accolades.CompletedAccolades.Add(new AccoladeEntry
        {
            Accolade = new DataTableRef { RowName = rowName, DataTableName = AccoladesDataTable },
            TimeCompleted = _clock.UtcNow.ToString("yyyy.MM.dd-HH.mm.ss", CultureInfo.InvariantCulture),
            ProspectID = "",
        });
        return true;
    }

    /// <summary>Removes a completed accolade; returns whether it was present.</summary>
    public bool RemoveAccolade(AccoladesModel accolades, string rowName)
    {
        ArgumentNullException.ThrowIfNull(accolades);
        ArgumentException.ThrowIfNullOrEmpty(rowName);

        var existing = accolades.CompletedAccolades
            .FirstOrDefault(a => string.Equals(a.Accolade.RowName, rowName, StringComparison.Ordinal));
        return existing is not null && accolades.CompletedAccolades.Remove(existing);
    }

    /// <summary>Sets a creature group's scan points, adding the group if absent.</summary>
    public void SetBestiaryPoints(BestiaryModel bestiary, string rowName, long points)
    {
        ArgumentNullException.ThrowIfNull(bestiary);
        ArgumentException.ThrowIfNullOrEmpty(rowName);

        var existing = bestiary.BestiaryTracking
            .FirstOrDefault(b => string.Equals(b.BestiaryGroup.RowName, rowName, StringComparison.Ordinal));
        if (existing is not null)
        {
            existing.NumPoints = points;
        }
        else
        {
            bestiary.BestiaryTracking.Add(new BestiaryEntry
            {
                BestiaryGroup = new DataTableRef { RowName = rowName, DataTableName = BestiaryDataTable },
                NumPoints = points,
            });
        }
    }

    /// <summary>Removes a creature group from bestiary tracking; returns whether it was present.</summary>
    public bool RemoveBestiaryGroup(BestiaryModel bestiary, string rowName)
    {
        ArgumentNullException.ThrowIfNull(bestiary);
        ArgumentException.ThrowIfNullOrEmpty(rowName);

        var existing = bestiary.BestiaryTracking
            .FirstOrDefault(b => string.Equals(b.BestiaryGroup.RowName, rowName, StringComparison.Ordinal));
        return existing is not null && bestiary.BestiaryTracking.Remove(existing);
    }
}
