using FluentAssertions;
using IUUT.Core.Abstractions;
using IUUT.Core.GameTuning;
using IUUT.Core.Io;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class GameTuningServiceTests : IDisposable
{
    private readonly TempDir _temp = new();
    private readonly GameTuningCatalog _catalog = new();
    private readonly GameTuningService _service = new(
        new SafeSaveWriter(new BackupManager(FixedClock.Default), new SystemGuidProvider()));

    private string SaveRoot => _temp.Path;

    private void WriteEngineIni(string content)
    {
        var path = GameTuningService.ResolveEngineIniPath(SaveRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private EngineIni ReadEngineIni() =>
        EngineIni.Parse(File.ReadAllText(GameTuningService.ResolveEngineIniPath(SaveRoot)));

    [Fact]
    public void ReadCurrent_ReflectsExistingCvars()
    {
        WriteEngineIni("[ConsoleVariables]\r\nr.Fog=0\r\nsg.ShadowQuality=2");

        var states = _service.ReadCurrent(SaveRoot, _catalog);

        states.Single(s => s.Setting.Id == "disable-fog").Enabled.Should().BeTrue();
        var shadow = states.Single(s => s.Setting.Id == "shadow-quality");
        shadow.Enabled.Should().BeTrue();
        shadow.Value.Should().Be(2);
        states.Single(s => s.Setting.Id == "disable-motionblur").Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyAsync_WritesEnabled_RemovesDisabled_ClampsToStableMax_AndBacksUp()
    {
        WriteEngineIni("[ConsoleVariables]\r\nsg.ShadowQuality=2");
        var states = _service.ReadCurrent(SaveRoot, _catalog).ToList();
        states.Single(s => s.Setting.Id == "disable-volfog").Enabled = true;
        var fps = states.Single(s => s.Setting.Id == "max-fps");
        fps.Enabled = true;
        fps.Value = 99_999; // above the stable max
        states.Single(s => s.Setting.Id == "shadow-quality").Enabled = false; // was set → remove

        (await _service.ApplyAsync(SaveRoot, states)).Should().BeTrue();

        var ini = ReadEngineIni();
        ini.GetValue("ConsoleVariables", "r.VolumetricFog").Should().Be("0");
        ini.GetValue("ConsoleVariables", "t.MaxFPS").Should().Be("360", "the value is clamped to the stable max");
        ini.GetValue("ConsoleVariables", "sg.ShadowQuality").Should().BeNull("a disabled setting is removed");

        var configDir = Path.GetDirectoryName(GameTuningService.ResolveEngineIniPath(SaveRoot))!;
        Directory.GetFiles(configDir, "*" + BackupManager.BackupInfix + "*").Should().NotBeEmpty("the existing Engine.ini is backed up");
    }

    [Fact]
    public async Task ApplyAsync_NoExistingEngineIni_CreatesIt()
    {
        var states = _service.ReadCurrent(SaveRoot, _catalog).ToList();
        var fps = states.Single(s => s.Setting.Id == "max-fps");
        fps.Enabled = true;
        fps.Value = 120;

        (await _service.ApplyAsync(SaveRoot, states)).Should().BeTrue();

        File.Exists(GameTuningService.ResolveEngineIniPath(SaveRoot)).Should().BeTrue();
        ReadEngineIni().GetValue("ConsoleVariables", "t.MaxFPS").Should().Be("120");
    }

    public void Dispose() => _temp.Dispose();
}
