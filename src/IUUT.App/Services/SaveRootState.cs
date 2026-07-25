using System.IO;
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

    /// <summary>
    /// Bumps every time <see cref="Current"/> actually changes. Consumers record the version they
    /// loaded against and reload when it moved — singleton page VMs only auto-load once, so without
    /// this a root browsed on Home after a page's first display never reached it (review finding).
    /// </summary>
    public int Version { get; private set; }

    /// <summary>The current save root. Writes are ignored unless the value is a real, existing
    /// directory (Home's textbox writes through per keystroke — half-typed paths must not become
    /// the app-wide root) and actually different.</summary>
    public string Current
    {
        get => _current;
        set
        {
            if (string.IsNullOrWhiteSpace(value) ||
                string.Equals(value, _current, StringComparison.OrdinalIgnoreCase) ||
                !Directory.Exists(value))
            {
                return;
            }

            _current = value;
            Version++;
        }
    }
}
