using FluentAssertions;
using IUUT.Core.DataPak;
using IUUT.Core.Services;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Gates game and save discovery. The failure that matters is the quiet one: IUUT not finding the
/// game and simply not refreshing, or forgetting where the user pointed it. Every result therefore
/// says where it came from, and a stale manual choice is reported rather than silently replaced.
/// </summary>
public sealed class GameLocationsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "iuut-locations-" + Guid.NewGuid().ToString("N"));

    public GameLocationsTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private string Dir(params string[] parts)
    {
        var path = Path.Combine([_root, .. parts]);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string Pak(string library)
    {
        var pak = Path.Combine(library, DataPakLocator.RelativePakPath);
        Directory.CreateDirectory(Path.GetDirectoryName(pak)!);
        File.WriteAllText(pak, "pak");
        return pak;
    }

    private string SteamWithLibrary(string libraryPath)
    {
        var steam = Dir("Steam");
        Dir("Steam", "steamapps");
        var escaped = libraryPath.Replace("\\", "\\\\", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(steam, "steamapps", "libraryfolders.vdf"),
            "\"libraryfolders\"\n{\n  \"0\"\n  {\n    \"path\" \"" + escaped + "\"\n  }\n}\n");
        return steam;
    }

    [Fact]
    public void FindGame_InTheSteamInstall_IsReportedAsTheDefault()
    {
        var steam = Dir("Steam");
        var pak = Pak(steam);

        var found = GameLocations.FindGame(null, steam);

        found.Path.Should().Be(pak);
        found.Source.Should().Be(LocationSource.Default);
    }

    [Fact]
    public void FindGame_OnAnotherDrive_IsFoundThroughTheLibraryList()
    {
        // The exact case that prompted this: the game moved to D:\SteamLibrary.
        var library = Dir("D-SteamLibrary");
        var pak = Pak(library);
        var steam = SteamWithLibrary(library);

        var found = GameLocations.FindGame(null, steam);

        found.Path.Should().Be(pak);
        found.Source.Should().Be(LocationSource.SteamLibrary);
        found.Detail.Should().Contain(library);
    }

    [Fact]
    public void FindGame_WhenNowhere_SaysSoAndHowToFixIt()
    {
        var found = GameLocations.FindGame(null, Dir("EmptySteam"));

        found.Found.Should().BeFalse();
        found.Source.Should().Be(LocationSource.NotFound);
        found.Detail.Should().Contain("Browse");
    }

    [Fact]
    public void FindGame_ManualChoiceWins_OverAutoDetection()
    {
        var steam = Dir("Steam");
        Pak(steam);
        var elsewhere = Pak(Dir("Elsewhere"));

        var found = GameLocations.FindGame(Dir("Elsewhere"), steam);

        found.Path.Should().Be(elsewhere);
        found.Source.Should().Be(LocationSource.Manual);
    }

    [Fact]
    public void FindGame_StaleManualChoice_IsReported_NotSilentlyReplaced()
    {
        var steam = Dir("Steam");
        Pak(steam);

        var found = GameLocations.FindGame(Path.Combine(_root, "Gone"), steam);

        found.Found.Should().BeFalse("the user chose a location; quietly using another one would hide that it broke");
        found.Detail.Should().Contain("no longer contains Icarus");
    }

    [Theory]
    [InlineData("library")]
    [InlineData("install")]
    [InlineData("content")]
    [InlineData("pak")]
    public void NormalizeGamePick_AcceptsWhicheverFolderTheUserThinksIsTheGame(string pickKind)
    {
        var library = Dir("Lib");
        var pak = Pak(library);
        var install = Path.Combine(library, "steamapps", "common", "Icarus");

        var picked = pickKind switch
        {
            "library" => library,
            "install" => install,
            "content" => Path.Combine(install, "Icarus", "Content"),
            _ => pak,
        };

        GameLocations.NormalizeGamePick(picked).Should().Be(pak);
    }

    [Fact]
    public void NormalizeGamePick_RejectsAFolderWithoutTheGame() =>
        GameLocations.NormalizeGamePick(Dir("Random")).Should().BeNull();

    [Fact]
    public void FindSaves_StandardLocation_IsTheDefault()
    {
        var appData = Dir("AppData");
        Dir("AppData", "Icarus", "Saved", "PlayerData");

        var found = GameLocations.FindSaves(null, Dir("Steam"), appData);

        found.Path.Should().Be(Path.Combine(appData, "Icarus", "Saved"));
        found.Source.Should().Be(LocationSource.Default);
    }

    [Fact]
    public void FindSaves_FallsBackToADedicatedServerInstall()
    {
        var library = Dir("ServerLib");
        var saved = Dir("ServerLib", "steamapps", "common", GameLocations.DedicatedServerFolder, "Icarus", "Saved");
        Dir("ServerLib", "steamapps", "common", GameLocations.DedicatedServerFolder, "Icarus", "Saved", "PlayerData");
        var steam = SteamWithLibrary(library);

        var found = GameLocations.FindSaves(null, steam, Dir("EmptyAppData"));

        found.Path.Should().Be(saved);
        found.Source.Should().Be(LocationSource.DedicatedServer);
    }

    [Theory]
    [InlineData("saved")]
    [InlineData("playerdata")]
    [InlineData("profile")]
    [InlineData("prospects")]
    [InlineData("icarus")]
    public void NormalizeSavePick_AcceptsWhicheverFolderTheUserThinksIsTheSaves(string pickKind)
    {
        var saved = Dir("Host", "Icarus", "Saved");
        Dir("Host", "Icarus", "Saved", "PlayerData", "70000000000000001", "Prospects");

        var picked = pickKind switch
        {
            "saved" => saved,
            "playerdata" => Path.Combine(saved, "PlayerData"),
            "profile" => Path.Combine(saved, "PlayerData", "70000000000000001"),
            "prospects" => Path.Combine(saved, "PlayerData", "70000000000000001", "Prospects"),
            _ => Path.Combine(_root, "Host", "Icarus"),
        };

        GameLocations.NormalizeSavePick(picked).Should().Be(saved);
    }

    [Fact]
    public void NormalizeSavePick_RejectsAFolderWithoutSaves() =>
        GameLocations.NormalizeSavePick(Dir("NotSaves")).Should().BeNull();

    [Fact]
    public void NormalizeSavePick_NeverClimbsOutOfTheFolderTheUserChose()
    {
        // The trap this guards: an unrelated folder under AppData\Local climbed its way up to the
        // real %LOCALAPPDATA%\Icarus\Saved and "found" saves the user never pointed at.
        Dir("Local", "Icarus", "Saved", "PlayerData");
        var unrelated = Dir("Local", "Temp", "some-download");

        GameLocations.NormalizeSavePick(unrelated).Should().BeNull();
    }

    [Fact]
    public void NormalizeGamePick_NeverClimbsOutOfTheFolderTheUserChose()
    {
        var steam = Dir("Steam");
        Pak(steam);
        var unrelated = Dir("Steam", "music", "soundtrack");

        GameLocations.NormalizeGamePick(unrelated).Should().BeNull();
    }

    [Fact]
    public void Libraries_DoesNotListTheSteamInstallTwice()
    {
        var steam = Dir("Steam");
        Dir("Steam", "steamapps");
        var escaped = steam.Replace("\\", "\\\\", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(steam, "steamapps", "libraryfolders.vdf"),
            "\"libraryfolders\"\n{\n  \"0\" { \"path\" \"" + escaped + "\" }\n}\n");

        DataPakLocator.Libraries(steam).Should().ContainSingle();
    }
}
