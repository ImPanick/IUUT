using IUUT.Core.Services;

namespace IUUT.App.Services;

/// <summary>
/// The one shared "which Icarus <c>Saved</c> folder are we working on" value. Home's Browse/textbox
/// writes it; Custom, Recovery, and the Game Tuner read it on every load — previously each screen
/// hardcoded the default root, so a non-default Steam-library path chosen on Home never reached
/// them ("No save profiles found"). Registered as a DI singleton.
/// </summary>
public sealed class SaveRootState
{
    private string _current = HomeService.DefaultSaveRoot;

    /// <summary>The current save root; empty/whitespace writes are ignored (keeps the last good value).</summary>
    public string Current
    {
        get => _current;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _current = value;
            }
        }
    }
}
