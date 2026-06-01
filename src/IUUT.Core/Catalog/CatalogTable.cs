using System.Diagnostics.CodeAnalysis;

namespace IUUT.Core.Catalog;

/// <summary>
/// An in-memory catalog table (e.g. <c>D_Talents</c>) loaded from embedded JSON: row
/// lookup by <c>RowName</c> plus the table's version. Forward-compatible — unknown
/// RowNames simply aren't present; callers treat catalog data as display/lookup only
/// (never gatekeeping, CONSTITUTION VI).
/// </summary>
public sealed class CatalogTable
{
    private readonly Dictionary<string, CatalogRow> _rowsByName;

    /// <summary>Creates a table from its rows (duplicate RowNames keep the first).</summary>
    public CatalogTable(string dataTable, string catalogVersion, IEnumerable<CatalogRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        DataTable = dataTable;
        CatalogVersion = catalogVersion;
        _rowsByName = rows
            .Where(r => !string.IsNullOrEmpty(r.RowName))
            .GroupBy(r => r.RowName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
    }

    /// <summary>The source data-table name, e.g. <c>D_Talents</c>.</summary>
    public string DataTable { get; }

    /// <summary>The catalog version stamp, e.g. <c>2026-02-mendel</c>.</summary>
    public string CatalogVersion { get; }

    /// <summary>Number of rows.</summary>
    public int Count => _rowsByName.Count;

    /// <summary>All rows.</summary>
    public IReadOnlyCollection<CatalogRow> Rows => _rowsByName.Values;

    /// <summary>All RowNames.</summary>
    public IEnumerable<string> RowNames => _rowsByName.Keys;

    /// <summary>Whether the table contains <paramref name="rowName"/>.</summary>
    public bool Contains(string rowName) => _rowsByName.ContainsKey(rowName);

    /// <summary>Looks up a row by name.</summary>
    public bool TryGet(string rowName, [MaybeNullWhen(false)] out CatalogRow row) =>
        _rowsByName.TryGetValue(rowName, out row);

    /// <summary>The display label for <paramref name="rowName"/>, falling back to a humanized RowName.</summary>
    public string Label(string rowName) =>
        _rowsByName.TryGetValue(rowName, out var row) ? row.Label : CatalogName.Humanize(rowName);
}
