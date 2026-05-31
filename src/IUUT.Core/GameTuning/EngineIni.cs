using System.Text;

namespace IUUT.Core.GameTuning;

/// <summary>
/// A minimal, line-preserving reader/writer for UE <c>Engine.ini</c> (master §20.1,
/// docs/GAME-TUNING.md). Keeps every original line verbatim — comments, blanks, sections, and
/// keys IUUT doesn't manage — so editing one cvar never disturbs the rest of the file. Only the
/// keys IUUT sets/removes change. Section names match exactly; cvar keys match case-insensitively.
/// </summary>
public sealed class EngineIni
{
    private readonly List<Line> _lines = [];

    private sealed class Line
    {
        public required string Raw { get; set; }
        public string? Section { get; init; }
        public string? Key { get; init; }
    }

    /// <summary>Parses INI text, preserving every line.</summary>
    public static EngineIni Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var ini = new EngineIni();
        if (text.Length == 0)
        {
            return ini; // empty / missing Engine.ini → an empty document
        }

        foreach (var raw in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var trimmed = raw.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                ini._lines.Add(new Line { Raw = raw, Section = trimmed[1..^1] });
            }
            else if (!trimmed.StartsWith(';') && !trimmed.StartsWith('#') && trimmed.Contains('=', StringComparison.Ordinal))
            {
                var key = trimmed[..trimmed.IndexOf('=', StringComparison.Ordinal)].Trim();
                ini._lines.Add(new Line { Raw = raw, Key = key });
            }
            else
            {
                ini._lines.Add(new Line { Raw = raw });
            }
        }

        return ini;
    }

    /// <summary>Returns the value of <paramref name="key"/> under <paramref name="section"/>, or <c>null</c> if absent.</summary>
    public string? GetValue(string section, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(section);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var inSection = false;
        foreach (var line in _lines)
        {
            if (line.Section is not null)
            {
                inSection = string.Equals(line.Section, section, StringComparison.Ordinal);
            }
            else if (inSection && line.Key is not null && string.Equals(line.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                var eq = line.Raw.IndexOf('=', StringComparison.Ordinal);
                return eq >= 0 ? line.Raw[(eq + 1)..].Trim() : "";
            }
        }

        return null;
    }

    /// <summary>Sets <paramref name="key"/>=<paramref name="value"/> under <paramref name="section"/>, creating the key or section if needed.</summary>
    public void SetValue(string section, string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(section);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        var sectionStart = -1;
        var sectionEnd = _lines.Count; // exclusive
        for (var i = 0; i < _lines.Count; i++)
        {
            if (_lines[i].Section is null)
            {
                continue;
            }

            if (sectionStart < 0 && string.Equals(_lines[i].Section, section, StringComparison.Ordinal))
            {
                sectionStart = i;
            }
            else if (sectionStart >= 0)
            {
                sectionEnd = i;
                break;
            }
        }

        if (sectionStart < 0)
        {
            // Append a new section + key at the end.
            if (_lines.Count > 0 && _lines[^1].Raw.Trim().Length > 0)
            {
                _lines.Add(new Line { Raw = "" });
            }

            _lines.Add(new Line { Raw = $"[{section}]", Section = section });
            _lines.Add(new Line { Raw = $"{key}={value}", Key = key });
            return;
        }

        for (var i = sectionStart + 1; i < sectionEnd; i++)
        {
            if (_lines[i].Key is not null && string.Equals(_lines[i].Key, key, StringComparison.OrdinalIgnoreCase))
            {
                _lines[i].Raw = $"{key}={value}";
                return;
            }
        }

        // Key absent in the section — insert at the end of the section (after the last non-blank line).
        var insertAt = sectionEnd;
        while (insertAt - 1 > sectionStart && _lines[insertAt - 1].Raw.Trim().Length == 0)
        {
            insertAt--;
        }

        _lines.Insert(insertAt, new Line { Raw = $"{key}={value}", Key = key });
    }

    /// <summary>Removes <paramref name="key"/> from <paramref name="section"/>; returns whether it was present.</summary>
    public bool RemoveKey(string section, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(section);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var inSection = false;
        for (var i = 0; i < _lines.Count; i++)
        {
            if (_lines[i].Section is not null)
            {
                inSection = string.Equals(_lines[i].Section, section, StringComparison.Ordinal);
            }
            else if (inSection && _lines[i].Key is not null && string.Equals(_lines[i].Key, key, StringComparison.OrdinalIgnoreCase))
            {
                _lines.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>Serializes back to INI text (CRLF line endings, the UE convention).</summary>
    public string ToText()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < _lines.Count; i++)
        {
            builder.Append(_lines[i].Raw);
            if (i < _lines.Count - 1)
            {
                builder.Append("\r\n");
            }
        }

        return builder.ToString();
    }
}
