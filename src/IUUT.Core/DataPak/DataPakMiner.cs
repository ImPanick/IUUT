using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace IUUT.Core.DataPak;

/// <summary>One gameplay DataTable mined from <c>data.pak</c>: its RowStruct name and raw JSON.</summary>
public sealed record MinedTable(string RowStruct, string Json, int ApproxRows);

/// <summary>
/// C# port of <c>scripts/extract-datapak.ps1</c> — the runtime catalog self-refresh miner
/// (elevation roadmap Tier 1). The game's <c>data.pak</c> is a run of zlib blocks
/// (<c>78 9C/DA/01</c>) whose inflated concatenation is ~40 MB of JSON DataTables; each table is a
/// top-level <c>{RowStruct, Defaults, Rows[]}</c> object. This inflates every block, splits the
/// top-level objects with a string/escape-aware brace scanner, and returns each table by its
/// RowStruct name. Fully offline — reads a local file the user already owns (CONSTITUTION V).
/// </summary>
public static class DataPakMiner
{
    private static readonly Regex _rowStructRegex = new(
        "\"RowStruct\"\\s*:\\s*\"/Script/Icarus\\.([^\"]+)\"", RegexOptions.Compiled);

    /// <summary>Mines every DataTable from raw <c>data.pak</c> bytes.</summary>
    public static IReadOnlyList<MinedTable> Mine(byte[] pakBytes, IProgress<string>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(pakBytes);

        progress?.Report("Inflating data.pak blocks…");
        var text = InflateAll(pakBytes);

        progress?.Report("Splitting DataTables…");
        var tables = new List<MinedTable>();
        foreach (var json in SplitTopLevelObjects(text))
        {
            var match = _rowStructRegex.Match(json);
            if (!match.Success)
            {
                continue;
            }

            var rows = CountOccurrences(json, "\"Name\":");
            tables.Add(new MinedTable(match.Groups[1].Value, json, rows));
        }

        progress?.Report($"Mined {tables.Count} DataTables.");
        return tables;
    }

    /// <summary>Mines a <c>data.pak</c> file from disk.</summary>
    /// <exception cref="FileNotFoundException">The pak file does not exist.</exception>
    public static IReadOnlyList<MinedTable> MineFile(string dataPakPath, IProgress<string>? progress = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataPakPath);
        return Mine(File.ReadAllBytes(dataPakPath), progress);
    }

    // Inflate every zlib block (0x78 0x9C/DA/01) in file order and concatenate the output.
    // Mirrors the PowerShell extractor exactly: candidate headers that fail to inflate are skipped.
    private static string InflateAll(byte[] bytes)
    {
        using var full = new MemoryStream();
        for (var i = 0; i < bytes.Length - 2; i++)
        {
            if (bytes[i] != 0x78 ||
                (bytes[i + 1] != 0x9C && bytes[i + 1] != 0xDA && bytes[i + 1] != 0x01))
            {
                continue;
            }

            try
            {
                using var view = new MemoryStream(bytes, i + 2, bytes.Length - i - 2, writable: false);
                using var deflate = new DeflateStream(view, CompressionMode.Decompress);
                var before = full.Length;
                deflate.CopyTo(full);
                if (full.Length > before)
                {
                    i += 4; // step past this block's header so the scan resumes inside/after it
                }
            }
            catch (InvalidDataException)
            {
                // A false-positive header (0x78 0x9C occurring in compressed data) — keep scanning.
            }
        }

        return new UTF8Encoding(false).GetString(full.ToArray());
    }

    // Split concatenated JSON text into top-level {...} objects (string/escape aware).
    private static IEnumerable<string> SplitTopLevelObjects(string text)
    {
        var depth = 0;
        var start = -1;
        var inString = false;
        var escaped = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (c == '"')
            {
                inString = true;
            }
            else if (c == '{')
            {
                if (depth == 0)
                {
                    start = i;
                }

                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    yield return text.Substring(start, i - start + 1);
                    start = -1;
                }
            }
        }
    }

    private static int CountOccurrences(string text, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
