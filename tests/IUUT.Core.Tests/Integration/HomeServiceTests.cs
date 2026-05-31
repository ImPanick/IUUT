using FluentAssertions;
using IUUT.Core.Models;
using IUUT.Core.Serializers;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class HomeServiceTests : IDisposable
{
    private const string IdAlpha = "11111111111111111";
    private const string IdBravo = "22222222222222222";

    private readonly TempDir _temp = new();
    private readonly FakeLocalSteamNames _localNames = new();

    private string SaveRoot => _temp.Path;

    private HomeService BuildService(params string[] runningProcesses)
    {
        var resolver = new SteamProfileResolverService(
            new SteamProfileCache(),
            _localNames,
            FixedClock.Default);
        var detector = new GameProcessDetector(new FakeRunningProcesses(runningProcesses));
        return new HomeService(new SaveDiscoveryService(), resolver, detector);
    }

    private void CreateProfile(string steamId64, int characterCount)
    {
        var dir = Directory.CreateDirectory(Path.Combine(SaveRoot, SaveDiscoveryService.PlayerDataFolder, steamId64)).FullName;
        File.WriteAllText(Path.Combine(dir, "Profile.json"), "{}");

        var roster = Enumerable.Range(1, characterCount)
            .Select(slot => new CharacterModel { CharacterName = $"Char{slot}", ChrSlot = slot })
            .ToList();
        File.WriteAllText(Path.Combine(dir, "Characters.json"), CharactersSerializer.Serialize(roster));
    }

    [Fact]
    public async Task LoadAsync_DiscoversProfiles_AndLabelsWithPersonaName()
    {
        CreateProfile(IdAlpha, characterCount: 3);
        CreateProfile(IdBravo, characterCount: 1);
        _localNames[IdAlpha] = "Joseph";
        // IdBravo intentionally unresolved → falls back to the raw SteamID64.

        var state = await BuildService().LoadAsync(SaveRoot);

        state.SaveRootFound.Should().BeTrue();
        state.HasProfiles.Should().BeTrue();
        state.Slots.Should().HaveCount(2);

        var alpha = state.Slots.Single(s => s.SteamId64 == IdAlpha);
        alpha.DisplayLabel.Should().Be("Joseph");
        alpha.PersonaName.Should().Be("Joseph");
        alpha.NameSource.Should().Be(SteamNameSource.LocalVdf);
        alpha.CharacterCount.Should().Be(3);
        alpha.HasProfileJson.Should().BeTrue();
        alpha.LooksLikeSteamId64.Should().BeTrue();

        var bravo = state.Slots.Single(s => s.SteamId64 == IdBravo);
        bravo.DisplayLabel.Should().Be(IdBravo, "an unresolved name falls back to the SteamID64");
        bravo.PersonaName.Should().BeNull();
        bravo.NameSource.Should().Be(SteamNameSource.Fallback);
        bravo.CharacterCount.Should().Be(1);
    }

    [Fact]
    public async Task LoadAsync_OrdersSlotsBySteamId()
    {
        CreateProfile(IdBravo, characterCount: 1);
        CreateProfile(IdAlpha, characterCount: 1);

        var state = await BuildService().LoadAsync(SaveRoot);

        state.Slots.Select(s => s.SteamId64).Should().ContainInOrder(IdAlpha, IdBravo);
    }

    [Fact]
    public async Task LoadAsync_MissingPlayerData_ReturnsNotFound_WithEmptySlots()
    {
        // No PlayerData folder created under SaveRoot.
        var state = await BuildService().LoadAsync(SaveRoot);

        state.SaveRootFound.Should().BeFalse();
        state.HasProfiles.Should().BeFalse();
        state.Slots.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_GameRunning_IsSurfacedInState()
    {
        CreateProfile(IdAlpha, characterCount: 1);

        var state = await BuildService("explorer", "Icarus-3.0.12.152317-Shipping-DangerousHorizons")
            .LoadAsync(SaveRoot);

        state.Game.IsRunning.Should().BeTrue();
        state.Game.MatchedProcessNames.Should().ContainSingle()
            .Which.Should().Contain("Shipping");
    }

    [Fact]
    public async Task LoadAsync_GameNotRunning_ReportsNotRunning()
    {
        CreateProfile(IdAlpha, characterCount: 1);

        var state = await BuildService("explorer", "steam").LoadAsync(SaveRoot);

        state.Game.IsRunning.Should().BeFalse();
        state.Game.MatchedProcessNames.Should().BeEmpty();
    }

    public void Dispose() => _temp.Dispose();
}
