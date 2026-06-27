using FluentAssertions;
using IUUT.Core.GameTuning;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class GameTuningCatalogTests
{
    private readonly GameTuningCatalog _catalog = new();

    [Fact]
    public void Settings_HaveUniqueIds_AndNonEmptyKeys()
    {
        _catalog.Settings.Should().NotBeEmpty();
        _catalog.Settings.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        _catalog.Settings.Should().OnlyContain(s => s.Key.Length > 0 && s.Group.Length > 0);
    }

    [Theory]
    [InlineData("r.ContactShadows")]
    [InlineData("grass.DisableDynamicShadows")]
    [InlineData("r.Shadow.CSM.MaxCascades")]
    [InlineData("r.Streaming.PoolSize")]
    [InlineData("r.Streaming.LimitPoolSizeToVRAM")]
    [InlineData("r.VolumetricCloud")]
    [InlineData("r.MotionBlur.Scale")]
    [InlineData("r.RayTracing.Shadows")]
    [InlineData("r.RTXGI.DDGI")]
    public void Settings_IncludeDataminedCVars(string cvar)
    {
        _catalog.Settings.Should().Contain(s => s.Key == cvar, $"the datamined cvar {cvar} should be tunable");
    }

    [Fact]
    public void RayTracing_AndTessellation_AreFlaggedExperimental()
    {
        var experimental = _catalog.Settings.Where(s => s.Experimental).Select(s => s.Key).ToList();

        experimental.Should().Contain("r.RayTracing.EnableInGame");
        experimental.Should().Contain("ShowFlag.Tessellation");
        _catalog.Settings.Where(s => s.Key.StartsWith("r.RayTracing.", StringComparison.Ordinal))
            .Should().OnlyContain(s => s.Experimental, "ray tracing may not apply in Icarus");
    }

    [Fact]
    public void NonExperimental_GraphicsTweaks_AreNotInTheAdvancedGroup()
    {
        var fog = _catalog.Settings.Single(s => s.Key == "r.Fog");
        fog.Experimental.Should().BeFalse();
        fog.Group.Should().NotContain("ADVANCED");
    }

    [Fact]
    public void GroupsCoverEveryTier()
    {
        var groups = _catalog.Settings.Select(s => s.Group).Distinct().ToList();
        groups.Should().Contain(["VISUAL FX", "FRAME RATE", "SHADOWS", "TEXTURES & STREAMING", "ADVANCED · MAY NOT APPLY"]);
    }
}
