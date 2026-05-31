using CommunityToolkit.Mvvm.ComponentModel;
using IUUT.Core.GameTuning;

namespace IUUT.App.ViewModels;

/// <summary>
/// Binding-shape for one tunable Engine.ini setting. <see cref="Value"/> clamps to the setting's
/// stable maximum, so both the slider and the free-text number box stay within the safe cap.
/// </summary>
public sealed class GameTuningSettingViewModel : ObservableObject
{
    private readonly GameTuningState _state;

    /// <summary>Wraps a tuning state.</summary>
    public GameTuningSettingViewModel(GameTuningState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
    }

    /// <summary>The underlying state (passed back to the service on apply).</summary>
    public GameTuningState State => _state;

    /// <summary>Setting label.</summary>
    public string Label => _state.Setting.Label;

    /// <summary>Setting description.</summary>
    public string Description => _state.Setting.Description;

    /// <summary>Whether this is a numeric setting (shows the slider + number box).</summary>
    public bool IsNumber => _state.Setting.Kind == GameTuningKind.Number;

    /// <summary>Slider minimum.</summary>
    public double Minimum => _state.Setting.Min;

    /// <summary>Slider maximum — the stable cap.</summary>
    public double Maximum => _state.Setting.StableMax;

    /// <summary>Slider step.</summary>
    public double Step => _state.Setting.Step;

    /// <summary>Optional unit suffix.</summary>
    public string Unit => _state.Setting.Unit ?? "";

    /// <summary>Whether IUUT manages this cvar (on → written; off → removed/game default).</summary>
    public bool Enabled
    {
        get => _state.Enabled;
        set
        {
            if (_state.Enabled != value)
            {
                _state.Enabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>The numeric value, clamped to [<see cref="Minimum"/>, <see cref="Maximum"/>] (the stable cap).</summary>
    public double Value
    {
        get => _state.Value;
        set
        {
            var clamped = Math.Clamp(value, Minimum, Maximum);
            if (Math.Abs(_state.Value - clamped) > double.Epsilon)
            {
                _state.Value = clamped;
                OnPropertyChanged();
            }
        }
    }
}
