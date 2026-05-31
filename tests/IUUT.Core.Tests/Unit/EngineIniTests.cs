using FluentAssertions;
using IUUT.Core.GameTuning;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class EngineIniTests
{
    private const string Sample =
        "[ConsoleVariables]\r\n" +
        "r.VolumetricFog=0\r\n" +
        "; a hand-written comment\r\n" +
        "r.ScreenPercentage=80\r\n" +
        "\r\n" +
        "[/Script/Engine.Engine]\r\n" +
        "bSmoothFrameRate=False";

    [Fact]
    public void GetValue_ReadsKeys_KeyCaseInsensitive_SectionExact()
    {
        var ini = EngineIni.Parse(Sample);

        ini.GetValue("ConsoleVariables", "r.VolumetricFog").Should().Be("0");
        ini.GetValue("ConsoleVariables", "R.VOLUMETRICFOG").Should().Be("0", "cvar keys are case-insensitive");
        ini.GetValue("ConsoleVariables", "r.Missing").Should().BeNull();
        ini.GetValue("/Script/Engine.Engine", "bSmoothFrameRate").Should().Be("False");
    }

    [Fact]
    public void SetValue_UpdatesExistingKey()
    {
        var ini = EngineIni.Parse(Sample);

        ini.SetValue("ConsoleVariables", "r.ScreenPercentage", "75");

        ini.GetValue("ConsoleVariables", "r.ScreenPercentage").Should().Be("75");
    }

    [Fact]
    public void SetValue_AddsKeyToExistingSection_PreservingComments()
    {
        var ini = EngineIni.Parse(Sample);

        ini.SetValue("ConsoleVariables", "r.Fog", "0");

        ini.GetValue("ConsoleVariables", "r.Fog").Should().Be("0");
        ini.ToText().Should().Contain("; a hand-written comment").And.Contain("r.ScreenPercentage=80");
    }

    [Fact]
    public void SetValue_AddsNewSectionWhenAbsent()
    {
        var ini = EngineIni.Parse(Sample);

        ini.SetValue("/Script/Engine.Player", "ConfiguredInternetSpeed", "104857600");

        ini.GetValue("/Script/Engine.Player", "ConfiguredInternetSpeed").Should().Be("104857600");
        ini.ToText().Should().Contain("[/Script/Engine.Player]");
    }

    [Fact]
    public void RemoveKey_ReportsPresence_AndPreservesTheRest()
    {
        var ini = EngineIni.Parse(Sample);

        ini.RemoveKey("ConsoleVariables", "r.VolumetricFog").Should().BeTrue();
        ini.RemoveKey("ConsoleVariables", "r.VolumetricFog").Should().BeFalse();

        ini.GetValue("ConsoleVariables", "r.VolumetricFog").Should().BeNull();
        ini.ToText().Should().Contain("; a hand-written comment").And.Contain("r.ScreenPercentage=80");
    }

    [Fact]
    public void Parse_Empty_RoundTripsToEmpty()
    {
        EngineIni.Parse("").ToText().Should().BeEmpty();
    }
}
