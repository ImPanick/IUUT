using System.Globalization;
using IUUT.Core.Abstractions;
using IUUT.Core.Catalog;
using IUUT.Core.Models;

namespace IUUT.Core.Services;

/// <summary>
/// "Lazy Max" — one-click, non-breaking maxing of the four core progression files
/// (master doc §11.4 / §12.2; field guide §4). This type performs the in-memory
/// mutation only; it does no I/O. Callers parse the save, run a <see cref="MaxAll"/>
/// pass, run the result through <see cref="Validation.ValidationEngine"/> (the gate),
/// and persist it through <see cref="Io.SafeSaveWriter"/> (atomic). Files outside the
/// four below are never touched (MetaInventory, Loadouts, Prospects, Mounts, flags, …).
/// </summary>
/// <remarks>
/// The talent strategy is the proven one (live Mendel save, 2026-05-12; master §8.3):
/// unlock the full account-union of player talents at <see cref="MaxTalentRank"/> and let
/// the game clamp each row independently to its true max on load (~71/9/10/10 % across
/// ranks 1–4). The union is computed from the live save at runtime — no catalog needed —
/// then the 16 functional <c>Genetics_*</c> rows are overlaid. Visual <c>*Reroute*</c>
/// path nodes carry no reward and are never added. The embedded catalog supplies the
/// account-wide unlock lists (workshop/prospect, accolades, bestiary). Unknown fields on
/// every touched record round-trip verbatim (CONSTITUTION VI) because existing entries are
/// mutated in place rather than rebuilt.
/// </remarks>
public sealed class LazyMaxService
{
    /// <summary>Rank applied to every unlocked character talent; the game clamps per row on load (master §8.3).</summary>
    public const int MaxTalentRank = 4;

    /// <summary>Lazy Max raises each character's XP to at least this (master §8.3, F-030: "XP ≥ 80,000,000").</summary>
    public const long MinMaxedExperience = 80_000_000;

    /// <summary>Rank for account-wide <c>Workshop_*</c>/<c>Prospect_*</c> unlocks (master §8.2: "Rank is always 1").</summary>
    public const int WorkshopUnlockRank = 1;

    /// <summary>
    /// Value every account currency is raised to (master §12.2 "Max MetaResources" — a high value the
    /// game clamps to its own per-currency cap on load, §3.2). One million is comfortably "maxed" for
    /// play yet far below any 32-bit overflow risk. Never lowers a currency that is already higher.
    /// </summary>
    public const long MaxedMetaResourceCount = 1_000_000;

    /// <summary>
    /// Value every bestiary group's <c>NumPoints</c> is raised to (master §8.5 "catalog max (or high
    /// value)"). Per-group true maxes vary (the live max observed was 1046); 10,000 clears every group's
    /// completion threshold. Refines to per-group catalog maxes once the bestiary catalog is enriched.
    /// </summary>
    public const long MaxedBestiaryPoints = 10_000;

    private const string AccoladesDataTable = "D_Accolades";
    private const string BestiaryDataTable = "D_BestiaryData";

    // The 16 functional Genetics rows (field guide §4.2.1). The three visual *Reroute* path nodes
    // (Genetics_Mutation_Reroute / _Reroute2 / _Reroute3) are deliberately excluded — they carry no
    // reward. Overlaid onto the runtime talent union so a fresh save still gets the full genetics tree.
    private static readonly string[] _geneticsTalents =
    [
        "Genetics_GestationSpeed",
        "Genetics_GestationBuff",
        "Genetics_RecoverySpeed",
        "Genetics_GenotypeMutation",
        "Genetics_GenotypeMutation2",
        "Genetics_PhenotypeMutation",
        "Genetics_PhenotypeMutation2",
        "Genetics_WildGenome",
        "Genetics_WildPhenome",
        "Genetics_WildBloodline",
        "Genetics_SireBuff",
        "Genetics_MaternalBuff",
        "Genetics_Twins",
        "Genetics_Lineage",
        "Genetics_Experience",
        "Genetics_Reduced_Threat",
    ];

    /// <summary>The 16 functional <c>Genetics_*</c> rows overlaid on every maxed character (field guide §4.2.1).</summary>
    public static IReadOnlyList<string> GeneticsTalents => _geneticsTalents;

    private readonly GameCatalogs _catalogs;
    private readonly IClock _clock;

    /// <summary>Creates the service with the embedded catalogs and a clock (for accolade timestamps).</summary>
    public LazyMaxService(GameCatalogs catalogs, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        ArgumentNullException.ThrowIfNull(clock);
        _catalogs = catalogs;
        _clock = clock;
    }

    /// <summary>
    /// Maxes all four core files in place and returns a summary. The mutation order mirrors the
    /// recovery order (master §12.1): Profile, then Characters, then Accolades, then Bestiary.
    /// </summary>
    public LazyMaxResult MaxAll(
        ProfileModel profile,
        IReadOnlyList<CharacterModel> characters,
        AccoladesModel accolades,
        BestiaryModel bestiary)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(characters);
        ArgumentNullException.ThrowIfNull(accolades);
        ArgumentNullException.ThrowIfNull(bestiary);

        var (metaMaxed, workshopTotal, workshopAdded) = MaxProfile(profile);
        var talentsPerCharacter = MaxCharacters(characters);
        var accoladesAdded = MaxAccolades(accolades);
        var (bestiaryTotal, bestiaryAdded) = MaxBestiary(bestiary);

        return new LazyMaxResult
        {
            CharactersMaxed = characters.Count,
            TalentsPerCharacter = talentsPerCharacter,
            MetaResourcesMaxed = metaMaxed,
            WorkshopUnlocksTotal = workshopTotal,
            WorkshopUnlocksAdded = workshopAdded,
            AccoladesAdded = accoladesAdded,
            BestiaryGroupsTotal = bestiaryTotal,
            BestiaryGroupsAdded = bestiaryAdded,
        };
    }

    /// <summary>
    /// Maxes every character: applies the account-union of talents (plus the 16 Genetics rows) at
    /// <see cref="MaxTalentRank"/>, raises XP to at least <see cref="MinMaxedExperience"/>, clears
    /// <c>XP_Debt</c>, and revives (<c>IsDead</c>/<c>IsAbandoned</c> → false). Returns the union size
    /// (talents now on every character).
    /// </summary>
    public int MaxCharacters(IReadOnlyList<CharacterModel> characters)
    {
        ArgumentNullException.ThrowIfNull(characters);

        // Union of all player-talent RowNames across every character (excluding visual Reroute nodes),
        // plus the functional Genetics rows. Sorted for deterministic output ordering.
        var union = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var character in characters)
        {
            foreach (var talent in character.Talents)
            {
                if (!string.IsNullOrEmpty(talent.RowName) && !IsReroute(talent.RowName))
                {
                    union.Add(talent.RowName);
                }
            }
        }

        foreach (var genetics in _geneticsTalents)
        {
            union.Add(genetics);
        }

        foreach (var character in characters)
        {
            // Index existing talents so unknown per-row fields are preserved (CONSTITUTION VI):
            // bump the rank on the existing object rather than replacing it.
            var existing = new Dictionary<string, Talent>(StringComparer.Ordinal);
            foreach (var talent in character.Talents)
            {
                existing.TryAdd(talent.RowName, talent);
            }

            foreach (var rowName in union)
            {
                if (existing.TryGetValue(rowName, out var talent))
                {
                    talent.Rank = MaxTalentRank;
                }
                else
                {
                    character.Talents.Add(new Talent { RowName = rowName, Rank = MaxTalentRank });
                }
            }

            character.XP = Math.Max(character.XP, MinMaxedExperience);
            character.XP_Debt = 0;
            character.IsDead = false;
            character.IsAbandoned = false;
        }

        return union.Count;
    }

    /// <summary>
    /// Maxes the profile: raises every account currency to <see cref="MaxedMetaResourceCount"/> (adding
    /// any catalog currency that is absent) and ensures every <c>Workshop_*</c>/<c>Prospect_*</c> catalog
    /// row is present at <see cref="WorkshopUnlockRank"/>. Returns (currencies maxed, workshop/prospect
    /// total ensured, workshop/prospect newly added).
    /// </summary>
    public (int MetaMaxed, int WorkshopTotal, int WorkshopAdded) MaxProfile(ProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        // Raise every existing currency (never lower one already higher), then add missing catalog rows.
        foreach (var resource in profile.MetaResources)
        {
            resource.Count = Math.Max(resource.Count, MaxedMetaResourceCount);
        }

        var existingRows = new HashSet<string>(
            profile.MetaResources.Select(m => m.MetaRow),
            StringComparer.Ordinal);
        foreach (var rowName in _catalogs.MetaResources.RowNames)
        {
            if (existingRows.Add(rowName))
            {
                profile.MetaResources.Add(new MetaResource { MetaRow = rowName, Count = MaxedMetaResourceCount });
            }
        }

        var existingTalents = new Dictionary<string, Talent>(StringComparer.Ordinal);
        foreach (var talent in profile.Talents)
        {
            existingTalents.TryAdd(talent.RowName, talent);
        }

        var total = 0;
        var added = 0;
        foreach (var rowName in _catalogs.Talents.RowNames.Where(IsWorkshopOrProspect))
        {
            total++;
            if (existingTalents.TryGetValue(rowName, out var talent))
            {
                talent.Rank = WorkshopUnlockRank;
            }
            else
            {
                profile.Talents.Add(new Talent { RowName = rowName, Rank = WorkshopUnlockRank });
                added++;
            }
        }

        return (profile.MetaResources.Count, total, added);
    }

    /// <summary>
    /// Appends every catalog accolade not already in <see cref="AccoladesModel.CompletedAccolades"/>
    /// (master §8.4). <c>PlayerTrackers</c>/<c>PlayerTaskListTrackers</c> are left verbatim. Returns the
    /// number appended.
    /// </summary>
    public int MaxAccolades(AccoladesModel accolades)
    {
        ArgumentNullException.ThrowIfNull(accolades);

        var present = new HashSet<string>(
            accolades.CompletedAccolades
                .Select(a => a.Accolade.RowName)
                .Where(n => !string.IsNullOrEmpty(n)),
            StringComparer.Ordinal);

        // Game timestamp format YYYY.MM.DD-HH.MM.SS (field guide §3); single stamp for the whole pass.
        var timeCompleted = _clock.UtcNow.ToString("yyyy.MM.dd-HH.mm.ss", CultureInfo.InvariantCulture);

        var added = 0;
        foreach (var rowName in _catalogs.Accolades.RowNames)
        {
            if (present.Add(rowName))
            {
                accolades.CompletedAccolades.Add(new AccoladeEntry
                {
                    Accolade = new DataTableRef { RowName = rowName, DataTableName = AccoladesDataTable },
                    TimeCompleted = timeCompleted,
                    ProspectID = "",
                });
                added++;
            }
        }

        return added;
    }

    /// <summary>
    /// Raises every creature group's <c>NumPoints</c> to <see cref="MaxedBestiaryPoints"/> and adds any
    /// catalog group that is absent (master §8.5). <c>FishTracking</c> is left verbatim. Returns
    /// (total groups maxed, groups newly added).
    /// </summary>
    public (int Total, int Added) MaxBestiary(BestiaryModel bestiary)
    {
        ArgumentNullException.ThrowIfNull(bestiary);

        foreach (var entry in bestiary.BestiaryTracking)
        {
            entry.NumPoints = Math.Max(entry.NumPoints, MaxedBestiaryPoints);
        }

        var present = new HashSet<string>(
            bestiary.BestiaryTracking
                .Select(b => b.BestiaryGroup.RowName)
                .Where(n => !string.IsNullOrEmpty(n)),
            StringComparer.Ordinal);

        var added = 0;
        foreach (var rowName in _catalogs.Bestiary.RowNames)
        {
            if (present.Add(rowName))
            {
                bestiary.BestiaryTracking.Add(new BestiaryEntry
                {
                    BestiaryGroup = new DataTableRef { RowName = rowName, DataTableName = BestiaryDataTable },
                    NumPoints = MaxedBestiaryPoints,
                });
                added++;
            }
        }

        return (bestiary.BestiaryTracking.Count, added);
    }

    private static bool IsReroute(string rowName) =>
        rowName.Contains("Reroute", StringComparison.Ordinal);

    private static bool IsWorkshopOrProspect(string rowName) =>
        rowName.StartsWith("Workshop_", StringComparison.Ordinal) ||
        rowName.StartsWith("Prospect_", StringComparison.Ordinal);
}
