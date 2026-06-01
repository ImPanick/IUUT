using FluentAssertions;
using IUUT.Core.Prospects.World;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>Verifies inventory-slot owner classification from recorder component classes (real-save-derived).</summary>
public class SlotOwnerTests
{
    [Theory]
    [InlineData("/Script/Icarus.PlayerStateRecorderComponent", SlotOwnerKind.PlayerCarried)]
    [InlineData("/Script/Icarus.DeployableRecorderComponent", SlotOwnerKind.DeployedStorage)]
    [InlineData("/Script/Icarus.IcarusContainerManagerRecorderComponent", SlotOwnerKind.DeployedStorage)]
    [InlineData("/Script/Icarus.IcarusMountCharacterRecorderComponent", SlotOwnerKind.Mount)]
    [InlineData("/Script/Icarus.DrillRecorderComponent", SlotOwnerKind.Machine)]
    [InlineData("/Script/Icarus.EnzymeGeyserRecorderComponent", SlotOwnerKind.Machine)]
    [InlineData("/Script/Icarus.SomethingElse", SlotOwnerKind.Other)]
    [InlineData(null, SlotOwnerKind.Other)]
    public void Classify_MapsRecorderComponentClasses(string? componentClass, SlotOwnerKind expected) =>
        SlotOwner.Classify(componentClass).Should().Be(expected);

    [Theory]
    [InlineData("/Script/Icarus.PlayerStateRecorderComponent", true)]
    [InlineData("/Script/Icarus.DeployableRecorderComponent", true)]
    [InlineData("/Script/Icarus.IcarusMountCharacterRecorderComponent", true)]
    [InlineData("/Script/Icarus.DrillRecorderComponent", false)]
    [InlineData(null, false)]
    public void IsPlayerOwned_CarriedDeployedMount_AreTrue_MachinesFalse(string? componentClass, bool expected) =>
        SlotOwner.IsPlayerOwned(componentClass).Should().Be(expected);
}
