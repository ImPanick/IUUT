using System.Text.Json;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;

namespace IUUT.Core.Recovery;

/// <summary>
/// Rebuilds a valid JSON skeleton for a corrupt save file when no clean backup exists
/// (master doc §11.3 F-023). Only the four modeled files have a safe template; everything
/// else is refused (<see cref="TemplateRepairResult.Unsupported"/>) rather than risk writing
/// a structurally-wrong file. A skeleton loses data — the caller flags partial recovery and
/// points the user at Custom (F-024). If the supplied content actually parses, it is salvaged
/// as-is instead of being flattened.
/// </summary>
public sealed class TemplateRepairService
{
    /// <summary>The account-wide profile file.</summary>
    public const string ProfileFile = "Profile.json";

    /// <summary>The character roster file.</summary>
    public const string CharactersFile = "Characters.json";

    /// <summary>The accolade log file.</summary>
    public const string AccoladesFile = "Accolades.json";

    /// <summary>The bestiary scan file.</summary>
    public const string BestiaryFile = "BestiaryData.json";

    /// <summary>Builds a template-repair result for <paramref name="fileName"/> given its (corrupt) content.</summary>
    public TemplateRepairResult Repair(string fileName, string? corruptContent, string folderSteamId)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        return Path.GetFileName(fileName) switch
        {
            ProfileFile => RepairProfile(corruptContent, folderSteamId),
            CharactersFile => RepairCharacters(corruptContent),
            AccoladesFile => RepairAccolades(corruptContent),
            BestiaryFile => RepairBestiary(corruptContent),
            var other => TemplateRepairResult.Unsupported(other),
        };
    }

    private static TemplateRepairResult RepairProfile(string? content, string folderSteamId)
    {
        if (TryParse(content, ProfileParser.Parse, out var parsed))
        {
            return TemplateRepairResult.Salvage(ProfileSerializer.Serialize(parsed),
                "Profile JSON was intact; reserialized without changes.");
        }

        var skeleton = new ProfileModel { UserId = folderSteamId };
        return TemplateRepairResult.Skeleton(ProfileSerializer.Serialize(skeleton),
            "Rebuilt a minimal Profile (UserID only). Currencies, unlocks and talents were lost — restore them via Custom.");
    }

    private static TemplateRepairResult RepairCharacters(string? content)
    {
        if (TryParse(content, CharactersParser.Parse, out var roster))
        {
            return TemplateRepairResult.Salvage(CharactersSerializer.Serialize(roster),
                "Characters JSON was intact; reserialized without changes.");
        }

        return TemplateRepairResult.Skeleton(CharactersSerializer.Serialize(new List<CharacterModel>()),
            "Rebuilt an empty character roster — characters were lost; recreate them in-game or via Custom.");
    }

    private static TemplateRepairResult RepairAccolades(string? content)
    {
        if (TryParse(content, AccoladesParser.Parse, out var parsed))
        {
            return TemplateRepairResult.Salvage(AccoladesSerializer.Serialize(parsed),
                "Accolades JSON was intact; reserialized without changes.");
        }

        return TemplateRepairResult.Skeleton(AccoladesSerializer.Serialize(new AccoladesModel()),
            "Rebuilt an empty accolade log — completed accolades were lost (re-grant via Lazy Max or Custom).");
    }

    private static TemplateRepairResult RepairBestiary(string? content)
    {
        if (TryParse(content, BestiaryParser.Parse, out var parsed))
        {
            return TemplateRepairResult.Salvage(BestiarySerializer.Serialize(parsed),
                "Bestiary JSON was intact; reserialized without changes.");
        }

        return TemplateRepairResult.Skeleton(BestiarySerializer.Serialize(new BestiaryModel()),
            "Rebuilt empty bestiary tracking — scan progress was lost (re-grant via Lazy Max or Custom).");
    }

    private static bool TryParse<T>(string? content, Func<string, T> parse, out T value)
        where T : class
    {
        value = null!;
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            value = parse(content);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
