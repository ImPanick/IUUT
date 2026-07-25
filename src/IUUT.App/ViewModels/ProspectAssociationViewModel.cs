namespace IUUT.App.ViewModels;

/// <summary>
/// One prospect association in the Prospects editor: the raw save id (the unstick key, always
/// shown) enriched with the catalog drop name when the D_ProspectList catalog knows the row.
/// </summary>
public sealed class ProspectAssociationViewModel
{
    /// <summary>Creates the row.</summary>
    public ProspectAssociationViewModel(string prospectId, string? catalogLabel)
    {
        ArgumentException.ThrowIfNullOrEmpty(prospectId);
        ProspectId = prospectId;
        CatalogLabel = catalogLabel;
    }

    /// <summary>The raw association id from the slot file (what unstick removes).</summary>
    public string ProspectId { get; }

    /// <summary>The catalog drop name (e.g. "ARCWOOD: Outpost"), when known.</summary>
    public string? CatalogLabel { get; }

    /// <summary>List display: drop name + raw id, or just the raw id.</summary>
    public string Display => CatalogLabel is null ? ProspectId : $"{CatalogLabel} · {ProspectId}";
}
