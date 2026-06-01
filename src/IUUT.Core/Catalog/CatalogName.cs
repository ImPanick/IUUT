using System.Text;

namespace IUUT.Core.Catalog;

/// <summary>
/// Derives a human-readable label from a raw data-table <c>RowName</c> when the catalog has no
/// curated display name (master §15). Purely mechanical and offline (CONSTITUTION V): underscores
/// become spaces and word boundaries (camelCase, letter↔digit) are split, so
/// <c>Larkwell_Armor_Alpha_Chest</c> → "Larkwell Armor Alpha Chest" and <c>Envirosuit_Tier2</c> →
/// "Envirosuit Tier 2". Faithful to the RowName — never guesses a different name.
/// </summary>
public static class CatalogName
{
    /// <summary>Humanizes <paramref name="rowName"/> into a display label; returns it unchanged when empty.</summary>
    public static string Humanize(string? rowName)
    {
        if (string.IsNullOrWhiteSpace(rowName))
        {
            return rowName ?? "";
        }

        var spaced = rowName.Replace('_', ' ');
        var builder = new StringBuilder(spaced.Length + 8);
        for (var i = 0; i < spaced.Length; i++)
        {
            var c = spaced[i];
            if (i > 0)
            {
                var p = spaced[i - 1];
                var boundary =
                    (char.IsUpper(c) && char.IsLower(p)) ||   // aB → a B
                    (char.IsDigit(c) && char.IsLetter(p)) ||  // r2 → r 2
                    (char.IsLetter(c) && char.IsDigit(p));     // 2x → 2 x
                if (boundary && p != ' ' && c != ' ')
                {
                    builder.Append(' ');
                }
            }

            builder.Append(c);
        }

        // Collapse any runs of whitespace introduced by the replacements.
        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
