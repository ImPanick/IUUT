using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;
using IUUT.Core.Services;

namespace IUUT.Core.Validation;

/// <summary>
/// Pre-write validation rules (master doc §13). <b>Errors</b> block a write (§13.1);
/// <b>warnings</b> are surfaced for the user to confirm (§13.2). Pure functions over
/// the domain models — no I/O. The post-write "JSON round-trips" hard check is enforced
/// operationally by <c>SafeSaveWriter</c> (re-parse after write); these rules cover the
/// semantic checks a re-parse cannot catch.
/// </summary>
/// <remarks>
/// Over-ranked-talent detection here is coarse (Rank &gt; 4, the highest rank observed
/// across the whole talent table — field guide §4.2); precise per-row max-rank validation
/// arrives with the embedded catalog (WP-11/WP-21). The game clamps over-ranked rows on
/// load regardless, so this is a warning, never an error.
/// </remarks>
public static class ValidationEngine
{
    private const int HighestObservedTalentRank = 4;

    /// <summary>Validates a <see cref="ProfileModel"/> against its on-disk folder name.</summary>
    public static ValidationResult ValidateProfile(ProfileModel profile, string folderSteamId)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrEmpty(folderSteamId);

        var issues = new List<ValidationIssue>();

        // §13.1: Profile.UserID must equal the folder name (the game rejects a mismatch).
        if (!string.IsNullOrEmpty(profile.UserId) &&
            !string.Equals(profile.UserId, folderSteamId, StringComparison.Ordinal))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "profile-userid-mismatch",
                $"Profile.UserID '{profile.UserId}' does not match the folder name '{folderSteamId}'.",
                "Profile.json/UserID"));
        }

        foreach (var duplicate in profile.MetaResources
                     .GroupBy(m => m.MetaRow, StringComparer.Ordinal)
                     .Where(g => g.Count() > 1))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                "profile-duplicate-metarow",
                $"MetaResource '{duplicate.Key}' appears {duplicate.Count()} times; the game keeps the last.",
                "Profile.json/MetaResources"));
        }

        foreach (var negative in profile.MetaResources.Where(m => m.Count < 0))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                "profile-negative-currency",
                $"MetaResource '{negative.MetaRow}' has a negative count ({negative.Count}).",
                "Profile.json/MetaResources"));
        }

        return ValidationResult.FromIssues(issues);
    }

    /// <summary>Validates the character roster; pass <paramref name="profile"/> to enable the slot cross-check.</summary>
    public static ValidationResult ValidateCharacters(
        IReadOnlyList<CharacterModel> characters,
        ProfileModel? profile = null)
    {
        ArgumentNullException.ThrowIfNull(characters);

        var issues = new List<ValidationIssue>();

        // §13.1: ChrSlot must be unique within the file.
        foreach (var slot in characters.GroupBy(c => c.ChrSlot).Where(g => g.Count() > 1))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "characters-duplicate-chrslot",
                $"ChrSlot {slot.Key} is used by {slot.Count()} characters; slots must be unique.",
                "Characters.json/ChrSlot"));
        }

        foreach (var character in characters)
        {
            // §13.1: no duplicate talent RowName within a character.
            foreach (var dupTalent in character.Talents
                         .GroupBy(t => t.RowName, StringComparer.Ordinal)
                         .Where(g => g.Count() > 1))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "character-duplicate-talent",
                    $"Character '{character.CharacterName}' (slot {character.ChrSlot}) has duplicate talent '{dupTalent.Key}'.",
                    "Characters.json/Talents"));
            }

            // §13.2: over-ranked talents (coarse — see remarks). The game clamps on load.
            foreach (var overRanked in character.Talents.Where(t => t.Rank > HighestObservedTalentRank))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "character-overranked-talent",
                    $"Talent '{overRanked.RowName}' rank {overRanked.Rank} exceeds the highest observed rank ({HighestObservedTalentRank}); the game will clamp it.",
                    "Characters.json/Talents"));
            }

            // §13.2: a slot at/above NextChrSlot is inconsistent with the profile.
            if (profile is not null && character.ChrSlot >= profile.NextChrSlot)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "character-chrslot-exceeds-next",
                    $"ChrSlot {character.ChrSlot} is >= Profile.NextChrSlot ({profile.NextChrSlot}).",
                    "Characters.json/ChrSlot"));
            }
        }

        return ValidationResult.FromIssues(issues);
    }

    /// <summary>§13.1: a touched prospect blob's SHA-1 must match its recorded hash.</summary>
    public static ValidationResult ValidateProspectBlob(ProspectBlobModel blob)
    {
        ArgumentNullException.ThrowIfNull(blob);

        return ProspectBlobVerifier.VerifyHash(blob)
            ? ValidationResult.Ok
            : ValidationResult.FromIssues([
                new ValidationIssue(
                    ValidationSeverity.Error,
                    "prospect-blob-hash-mismatch",
                    "The prospect blob's SHA-1 does not match its recorded Hash.",
                    "Prospects/*.json/ProspectBlob"),
            ]);
    }

    /// <summary>§13.1: new <c>DatabaseGUID</c>s must be unique (e.g. MetaInventory items).</summary>
    public static ValidationResult ValidateUniqueDatabaseGuids(IEnumerable<string> databaseGuids)
    {
        ArgumentNullException.ThrowIfNull(databaseGuids);

        var issues = new List<ValidationIssue>();
        foreach (var duplicate in databaseGuids
                     .Where(g => !string.IsNullOrEmpty(g))
                     .GroupBy(g => g, StringComparer.OrdinalIgnoreCase)
                     .Where(g => g.Count() > 1))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "duplicate-database-guid",
                $"DatabaseGUID '{duplicate.Key}' is used by {duplicate.Count()} items; GUIDs must be unique.",
                "DatabaseGUID"));
        }

        return ValidationResult.FromIssues(issues);
    }

    /// <summary>
    /// §13.2 / §14: warns when the game is running. Never an error — IUUT must not
    /// hard-block on game state (CONSTITUTION IX); the user accepts the risk.
    /// </summary>
    public static ValidationResult ValidateGameState(GameDetectionResult game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return game.IsRunning
            ? ValidationResult.FromIssues([
                new ValidationIssue(
                    ValidationSeverity.Warning,
                    "game-running",
                    "Icarus appears to be running. Stay on the Main Menu only, or close the game, before saving.",
                    "global"),
            ])
            : ValidationResult.Ok;
    }
}
