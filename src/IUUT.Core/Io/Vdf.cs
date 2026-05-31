using System.Text;

namespace IUUT.Core.Io;

/// <summary>
/// A parsed Valve KeyValues (VDF) node — either a leaf string or an object with
/// named children. Minimal reader used for Steam's text VDF files (e.g.
/// <c>loginusers.vdf</c>). Keys are matched case-insensitively (Valve convention).
/// </summary>
public sealed class VdfNode
{
    private readonly Dictionary<string, VdfNode> _children;

    private VdfNode(string value)
    {
        Value = value;
        _children = new Dictionary<string, VdfNode>(StringComparer.OrdinalIgnoreCase);
    }

    private VdfNode(Dictionary<string, VdfNode> children) => _children = children;

    /// <summary>The leaf string value, or <c>null</c> if this node is an object.</summary>
    public string? Value { get; }

    /// <summary>Whether this node is an object (has children) rather than a leaf string.</summary>
    public bool IsObject => Value is null;

    /// <summary>Child nodes (empty for a leaf).</summary>
    public IReadOnlyDictionary<string, VdfNode> Children => _children;

    /// <summary>An empty object node.</summary>
    public static VdfNode EmptyObject { get; } = new(new Dictionary<string, VdfNode>(StringComparer.OrdinalIgnoreCase));

    /// <summary>Gets a child leaf string by key.</summary>
    public bool TryGetString(string key, out string value)
    {
        if (_children.TryGetValue(key, out var node) && node.Value is not null)
        {
            value = node.Value;
            return true;
        }

        value = "";
        return false;
    }

    /// <summary>Gets a child object by key.</summary>
    public bool TryGetObject(string key, out VdfNode node)
    {
        if (_children.TryGetValue(key, out var found) && found.IsObject)
        {
            node = found;
            return true;
        }

        node = EmptyObject;
        return false;
    }

    internal static VdfNode Leaf(string value) => new(value);

    internal static VdfNode Object(Dictionary<string, VdfNode> children) => new(children);
}

/// <summary>Minimal recursive-descent parser for Valve KeyValues (VDF) text.</summary>
public static class Vdf
{
    private enum Kind { String, Open, Close }

    private readonly record struct Token(Kind Kind, string Text);

    /// <summary>Parses VDF <paramref name="text"/> into a root object node.</summary>
    public static VdfNode Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var tokens = Tokenize(text);
        var index = 0;
        return ParseObject(tokens, ref index);
    }

    private static VdfNode ParseObject(List<Token> tokens, ref int index)
    {
        var children = new Dictionary<string, VdfNode>(StringComparer.OrdinalIgnoreCase);

        while (index < tokens.Count)
        {
            var token = tokens[index];
            if (token.Kind == Kind.Close)
            {
                index++;
                break;
            }

            if (token.Kind != Kind.String)
            {
                index++;
                continue;
            }

            var key = token.Text;
            index++;
            if (index >= tokens.Count)
            {
                break;
            }

            var next = tokens[index];
            if (next.Kind == Kind.Open)
            {
                index++;
                children[key] = ParseObject(tokens, ref index);
            }
            else if (next.Kind == Kind.String)
            {
                children[key] = VdfNode.Leaf(next.Text);
                index++;
            }
            else
            {
                index++;
            }
        }

        return VdfNode.Object(children);
    }

    private static List<Token> Tokenize(string s)
    {
        var tokens = new List<Token>();
        var i = 0;
        var n = s.Length;

        while (i < n)
        {
            var c = s[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '/' && i + 1 < n && s[i + 1] == '/')
            {
                while (i < n && s[i] != '\n')
                {
                    i++;
                }

                continue;
            }

            if (c == '{')
            {
                tokens.Add(new Token(Kind.Open, "{"));
                i++;
                continue;
            }

            if (c == '}')
            {
                tokens.Add(new Token(Kind.Close, "}"));
                i++;
                continue;
            }

            if (c == '"')
            {
                i++;
                var sb = new StringBuilder();
                while (i < n && s[i] != '"')
                {
                    if (s[i] == '\\' && i + 1 < n)
                    {
                        i++;
                        sb.Append(Unescape(s[i]));
                    }
                    else
                    {
                        sb.Append(s[i]);
                    }

                    i++;
                }

                i++; // consume closing quote
                tokens.Add(new Token(Kind.String, sb.ToString()));
                continue;
            }

            // Bare (unquoted) token.
            var bare = new StringBuilder();
            while (i < n && !char.IsWhiteSpace(s[i]) && s[i] != '{' && s[i] != '}')
            {
                bare.Append(s[i]);
                i++;
            }

            tokens.Add(new Token(Kind.String, bare.ToString()));
        }

        return tokens;
    }

    private static char Unescape(char c) => c switch
    {
        'n' => '\n',
        't' => '\t',
        '\\' => '\\',
        '"' => '"',
        _ => c,
    };
}
