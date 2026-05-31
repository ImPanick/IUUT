using FluentAssertions;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class GameProcessDetectorTests
{
    [Theory]
    [InlineData("Icarus-Win64-Shipping", true)]                              // older build
    [InlineData("Icarus-3.0.12.152317-Shipping-DangerousHorizons", true)]    // current (version + expansion)
    [InlineData("ICARUS-9.9.9-SHIPPING-FUTUREDLC", true)]                    // case-insensitive, future patch
    [InlineData("IcarusLauncher", false)]                                    // launcher: no "Shipping"
    [InlineData("Icarus", false)]                                            // bare, no "Shipping"
    [InlineData("steam", false)]
    [InlineData("NotIcarus-Shipping", false)]                                // must START with Icarus
    [InlineData("", false)]
    public void IsIcarusShippingProcessName_MatchesByPattern(string name, bool expected)
    {
        GameProcessDetector.IsIcarusShippingProcessName(name).Should().Be(expected);
    }

    [Fact]
    public void Detect_WhenShippingProcessPresent_ReportsRunningWithMatch()
    {
        var detector = new GameProcessDetector(new FakeRunningProcesses(
            "steam", "explorer", "Icarus-3.0.12.152317-Shipping-DangerousHorizons"));

        var result = detector.Detect();

        result.IsRunning.Should().BeTrue();
        result.MatchedProcessNames.Should().ContainSingle()
            .Which.Should().Be("Icarus-3.0.12.152317-Shipping-DangerousHorizons");
    }

    [Fact]
    public void Detect_WhenOnlyLauncherPresent_ReportsNotRunning()
    {
        var detector = new GameProcessDetector(new FakeRunningProcesses("IcarusLauncher", "steam"));

        detector.Detect().IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Detect_NoProcesses_ReportsNotRunning()
    {
        var detector = new GameProcessDetector(new FakeRunningProcesses());

        var result = detector.Detect();

        result.IsRunning.Should().BeFalse();
        result.MatchedProcessNames.Should().BeEmpty();
    }
}
