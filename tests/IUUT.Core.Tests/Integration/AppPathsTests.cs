using FluentAssertions;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class AppPathsTests : IDisposable
{
    private readonly TempDir _temp = new();

    private string ExeDir => Directory.CreateDirectory(Path.Combine(_temp.Path, "exe")).FullName;

    private string AppData => Directory.CreateDirectory(Path.Combine(_temp.Path, "appdata")).FullName;

    [Fact]
    public void Resolve_DefaultMode_UsesAppDataIuutFolder()
    {
        var paths = AppPaths.Resolve(ExeDir, AppData);

        paths.IsPortable.Should().BeFalse();
        paths.StateRoot.Should().Be(Path.Combine(AppData, "IUUT"));
        paths.SteamCacheFile.Should().Be(Path.Combine(AppData, "IUUT", "steam-profile-cache.json"));
        paths.LogsDirectory.Should().Be(Path.Combine(AppData, "IUUT", "Logs"));
    }

    [Fact]
    public void Resolve_PortableMarkerPresent_UsesDataFolderBesideExe()
    {
        var exe = ExeDir;
        File.WriteAllText(Path.Combine(exe, AppPaths.PortableMarkerFileName), "");

        var paths = AppPaths.Resolve(exe, AppData);

        paths.IsPortable.Should().BeTrue();
        paths.StateRoot.Should().Be(Path.Combine(exe, "IUUT-Data"));
        paths.SteamCacheFile.Should().StartWith(paths.StateRoot);
        paths.SettingsFile.Should().StartWith(paths.StateRoot);
    }

    [Fact]
    public void DerivedPaths_AreAllUnderStateRoot()
    {
        var paths = AppPaths.Resolve(ExeDir, AppData);

        new[] { paths.SteamCacheFile, paths.SettingsFile, paths.ApiKeyFile, paths.LogsDirectory, paths.RuntimeExtractDirectory }
            .Should().OnlyContain(p => p.StartsWith(paths.StateRoot, StringComparison.Ordinal));
    }

    [Fact]
    public void EnsureStateRoot_CreatesDirectory()
    {
        var paths = AppPaths.Resolve(ExeDir, AppData);
        Directory.Exists(paths.StateRoot).Should().BeFalse();

        paths.EnsureStateRoot();

        Directory.Exists(paths.StateRoot).Should().BeTrue();
    }

    public void Dispose() => _temp.Dispose();
}
