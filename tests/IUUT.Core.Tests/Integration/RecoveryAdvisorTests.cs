using FluentAssertions;
using IUUT.Core.Models;
using IUUT.Core.Recovery;
using IUUT.Core.Serializers;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class RecoveryAdvisorTests : IDisposable
{
    private const string SteamId = "11111111111111111";

    private readonly TempDir _temp = new();
    private readonly RecoveryAdvisor _advisor = new();
    private readonly string _dir;

    public RecoveryAdvisorTests() =>
        _dir = Directory.CreateDirectory(Path.Combine(_temp.Path, SteamId)).FullName;

    private void Write(string name, string content) => File.WriteAllText(Path.Combine(_dir, name), content);

    private void WriteProfile(int dataVersion = RecoveryAdvisor.KnownDataVersion, int nextChrSlot = 99) =>
        Write("Profile.json", ProfileSerializer.Serialize(new ProfileModel
        {
            UserId = SteamId,
            DataVersion = dataVersion,
            NextChrSlot = nextChrSlot,
        }));

    private void WriteCharacters(params int[] slots) =>
        Write("Characters.json", CharactersSerializer.Serialize(
            slots.Select(s => new CharacterModel { CharacterName = $"C{s}", ChrSlot = s }).ToList()));

    [Fact]
    public void Advise_CleanCoherentSave_NoEnvArtifacts_ReturnsNoAdvisories()
    {
        WriteProfile();
        WriteCharacters(1, 2);

        _advisor.Advise(_dir, []).Should().BeEmpty();
    }

    [Fact]
    public void Advise_SteamCloudManifestPresent_WarnsAboutCloud()
    {
        WriteProfile();
        Write("steam_autocloud.vdf", "\"AutoCloud\" { }");

        _advisor.Advise(_dir, []).Should().ContainSingle(s => s.Contains("Steam Cloud"));
    }

    [Fact]
    public void Advise_ConflictedCopyPresent_WarnsAboutCloudSync()
    {
        WriteProfile();
        Write("Profile (DESKTOP-AB12's conflicted copy 2026-05-30).json", "{}");

        _advisor.Advise(_dir, []).Should().Contain(s => s.Contains("conflicted copies", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Advise_WriteDenied_SuggestsControlledFolderAccess()
    {
        WriteProfile();
        var results = new[]
        {
            new RecoveryFileResult
            {
                RelativePath = "MetaInventory.json",
                Outcome = RecoveryOutcome.RestoreFromGameBackup,
                Changed = false,
                Failed = true,
                Error = "Access to the path 'MetaInventory.json' is denied.",
            },
        };

        _advisor.Advise(_dir, results).Should().Contain(s => s.Contains("Controlled folder access"));
    }

    [Fact]
    public void Advise_DataVersionMismatch_FlagsIncompatibilityNotCorruption()
    {
        WriteProfile(dataVersion: 3);

        _advisor.Advise(_dir, []).Should().Contain(s => s.Contains("DataVersion") && s.Contains("incompatibility"));
    }

    [Fact]
    public void Advise_IncoherentRestore_FlagsCrossFileMismatch()
    {
        WriteProfile(nextChrSlot: 1);  // profile says next slot is 1...
        WriteCharacters(5);            // ...but a character occupies slot 5

        _advisor.Advise(_dir, []).Should().Contain(s => s.Contains("different points in time"));
    }

    public void Dispose() => _temp.Dispose();
}
