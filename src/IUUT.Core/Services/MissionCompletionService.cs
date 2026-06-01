using IUUT.Core.Catalog;
using IUUT.Core.Models;

namespace IUUT.Core.Services;

/// <summary>
/// Marks Icarus missions complete on a <see cref="ProfileModel"/>. A mission = its <c>Prospect_*</c>
/// reward talent in <c>Profile.Talents</c>; "complete" = the profile owns that talent (master §8.8).
/// Completing a mission also completes its full transitive prerequisite closure
/// (<see cref="MissionCatalog.AllPrerequisites"/>), because the game's progression gates a mission on
/// owning every ancestor talent. Additive and idempotent: existing talents are never disturbed and
/// re-running grants nothing new.
/// </summary>
/// <remarks>
/// This handles the universal completion mechanism (the <c>Prospect_*</c> talent). The signature
/// account/character "milestone" flags (Nightfall, Ironclad, map-unlock gates) are a separate concern,
/// set by the flag editors and <c>LazyMaxService.MaxAccountMissionFlags</c>.
/// </remarks>
public sealed class MissionCompletionService
{
    /// <summary>Rank for a mission's <c>Prospect_*</c> account unlock (always 1, like workshop unlocks).</summary>
    public const int MissionTalentRank = 1;

    private readonly MissionCatalog _missions;

    /// <summary>Creates the service over the mission graph catalog.</summary>
    public MissionCompletionService(MissionCatalog missions)
    {
        ArgumentNullException.ThrowIfNull(missions);
        _missions = missions;
    }

    /// <summary>The outcome of a completion: what was requested and what changed.</summary>
    public sealed record MissionCompletionResult(
        int MissionsRequested,
        int TalentsRequired,
        int TalentsAdded,
        IReadOnlyList<string> CompletedRowNames);

    /// <summary>
    /// Completes <paramref name="missionRowNames"/> and every transitive prerequisite by ensuring each
    /// corresponding <c>Prospect_*</c> talent is present in <paramref name="profile"/>. Unknown rows are
    /// still granted (the prerequisite closure simply adds nothing for them). Returns what changed.
    /// </summary>
    public MissionCompletionResult Complete(ProfileModel profile, IEnumerable<string> missionRowNames)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(missionRowNames);

        var requested = new List<string>();
        var required = new HashSet<string>(StringComparer.Ordinal);
        foreach (var mission in missionRowNames)
        {
            if (string.IsNullOrEmpty(mission))
            {
                continue;
            }

            requested.Add(mission);
            required.Add(mission);
            foreach (var prereq in _missions.AllPrerequisites(mission))
            {
                required.Add(prereq);
            }
        }

        var existing = new HashSet<string>(
            profile.Talents.Where(t => !string.IsNullOrEmpty(t.RowName)).Select(t => t.RowName),
            StringComparer.Ordinal);

        var added = 0;
        foreach (var rowName in required)
        {
            if (existing.Add(rowName))
            {
                profile.Talents.Add(new Talent { RowName = rowName, Rank = MissionTalentRank });
                added++;
            }
        }

        return new MissionCompletionResult(
            requested.Count,
            required.Count,
            added,
            required.OrderBy(r => r, StringComparer.Ordinal).ToList());
    }

    /// <summary>Completes every mission in the catalog (the "complete all missions" Lazy-Max action).</summary>
    public MissionCompletionResult CompleteAll(ProfileModel profile) =>
        Complete(profile, _missions.Missions.Select(m => m.RowName));
}
