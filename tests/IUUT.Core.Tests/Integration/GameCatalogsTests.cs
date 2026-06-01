using FluentAssertions;
using IUUT.Core.Catalog;
using Xunit;

namespace IUUT.Core.Tests.Integration;

/// <summary>Verifies the embedded catalogs ship in IUUT.Catalog and load end to end.</summary>
public class GameCatalogsTests
{
    private static readonly GameCatalogs _catalogs = GameCatalogs.LoadEmbedded();

    [Fact]
    public void LoadEmbedded_LoadsAllFiveCatalogs()
    {
        _catalogs.Talents.DataTable.Should().Be("D_Talents");
        _catalogs.Items.DataTable.Should().Be("D_ItemsStatic");
        _catalogs.Accolades.DataTable.Should().Be("D_Accolades");
        _catalogs.Bestiary.DataTable.Should().Be("D_BestiaryData");
        _catalogs.MetaResources.DataTable.Should().Be("D_MetaResources");
    }

    [Fact]
    public void Talents_ContainTheExpectedSeedData()
    {
        _catalogs.Talents.Count.Should().BeGreaterThan(1000, "character talents + the complete workshop set");
        _catalogs.Talents.Contains("Workshop_Envirosuit").Should().BeTrue();
        _catalogs.Talents.Contains("Genetics_Twins").Should().BeTrue();
        _catalogs.Talents.Contains("Workshop_Biolab_Inhaler_Sandworm").Should().BeTrue("the Norex inhaler category, completed beyond the original 310");
        _catalogs.Talents.Contains("Workshop_Bulky_Armor_Chest").Should().BeTrue("bulky armor, completed beyond the original 310");
        _catalogs.Talents.Contains("Definitely_Not_A_Real_Talent").Should().BeFalse();
    }

    [Fact]
    public void Accolades_And_Bestiary_AreSeeded()
    {
        _catalogs.Accolades.Count.Should().Be(446, "the complete D_Accolades set (boss kills, milestones, ribbons)");
        _catalogs.Accolades.Contains("DefeatGarganutan").Should().BeTrue("a boss-kill accolade beyond the original 212");
        _catalogs.Accolades.Contains("BearsKilled5000").Should().BeTrue("a kill-X-times milestone");
        _catalogs.Bestiary.Count.Should().Be(78);
        _catalogs.Bestiary.Contains("Forest_Wolf").Should().BeTrue();
    }

    [Fact]
    public void MetaResources_HaveRealDisplayNames()
    {
        _catalogs.MetaResources.Count.Should().Be(7);
        _catalogs.MetaResources.Label("Credits").Should().Be("Ren Credits");
        _catalogs.MetaResources.Label("Exotic_Uranium").Should().Be("Uranium Exotic");
        _catalogs.MetaResources.Contains("Biomass").Should().BeTrue();
    }

    [Fact]
    public void Items_AreSeeded()
    {
        _catalogs.Items.Count.Should().BeGreaterThan(0);
        _catalogs.Items.CatalogVersion.Should().Be("2026-02-mendel");
    }
}
