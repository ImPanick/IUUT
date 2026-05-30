using FluentAssertions;
using IUUT.Core.Models;
using IUUT.Core.Serializers;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class SaveDiscoveryServiceTests : IDisposable
{
    private readonly TempDir _temp = new();
    private readonly SaveDiscoveryService _sut = new();

    /// <summary>Creates a PlayerData/&lt;id&gt; folder with optional Profile.json and a character roster.</summary>
    private string MakeProfile(string id, int? characters = null, bool withProfile = true, string? rawCharacters = null)
    {
        var dir = Path.Combine(_temp.Path, SaveDiscoveryService.PlayerDataFolder, id);
        Directory.CreateDirectory(dir);

        if (withProfile)
        {
            File.WriteAllText(Path.Combine(dir, "Profile.json"), """{ "UserID": "00000000000000000" }""");
        }

        if (rawCharacters is not null)
        {
            File.WriteAllText(Path.Combine(dir, "Characters.json"), rawCharacters);
        }
        else if (characters is int n)
        {
            var roster = Enumerable.Range(1, n)
                .Select(i => new CharacterModel { CharacterName = "Char" + i, ChrSlot = i })
                .ToList();
            File.WriteAllText(Path.Combine(dir, "Characters.json"), CharactersSerializer.Serialize(roster));
        }

        return dir;
    }

    [Fact]
    public void DiscoverProfiles_EnumeratesAndOrdersSubfolders()
    {
        MakeProfile("00000000000000002", characters: 1);
        MakeProfile("00000000000000001", characters: 3);

        var profiles = _sut.DiscoverProfiles(_temp.Path);

        profiles.Should().HaveCount(2);
        profiles.Select(p => p.SteamId64).Should().Equal("00000000000000001", "00000000000000002");
    }

    [Fact]
    public void DiscoverProfiles_CountsCharactersAndFlagsMetadata()
    {
        MakeProfile("00000000000000000", characters: 3);

        var p = _sut.DiscoverProfiles(_temp.Path).Single();

        p.CharacterCount.Should().Be(3);
        p.HasProfileJson.Should().BeTrue();
        p.LooksLikeSteamId64.Should().BeTrue();
        p.LastModifiedUtc.Should().NotBeNull();
    }

    [Fact]
    public void DiscoverProfiles_NonSteamIdFolderName_FlaggedFalse()
    {
        MakeProfile("not-a-steam-id", characters: 0, withProfile: false);

        var p = _sut.DiscoverProfiles(_temp.Path).Single();

        p.LooksLikeSteamId64.Should().BeFalse();
        p.HasProfileJson.Should().BeFalse();
    }

    [Fact]
    public void DiscoverProfiles_CorruptCharacters_CountIsNull()
    {
        MakeProfile("00000000000000000", rawCharacters: "this is not json");

        var p = _sut.DiscoverProfiles(_temp.Path).Single();

        p.CharacterCount.Should().BeNull("a corrupt Characters.json must not crash discovery");
    }

    [Fact]
    public void DiscoverProfiles_MissingCharacters_CountIsNull()
    {
        MakeProfile("00000000000000000", withProfile: true);

        _sut.DiscoverProfiles(_temp.Path).Single().CharacterCount.Should().BeNull();
    }

    [Fact]
    public void DiscoverProfiles_NoPlayerDataFolder_ReturnsEmpty()
    {
        _sut.DiscoverProfiles(_temp.Path).Should().BeEmpty();
        _sut.SaveRootContainsPlayerData(_temp.Path).Should().BeFalse();
    }

    [Fact]
    public void SaveRootContainsPlayerData_TrueWhenPresent()
    {
        MakeProfile("00000000000000000");

        _sut.SaveRootContainsPlayerData(_temp.Path).Should().BeTrue();
    }

    [Fact]
    public void ResolveDefaultSaveRoot_EndsWithIcarusSaved()
    {
        SaveDiscoveryService.ResolveDefaultSaveRoot()
            .Should().EndWith(Path.Combine("Icarus", "Saved"));
    }

    public void Dispose() => _temp.Dispose();
}
