namespace IUUT.Core.Services;

/// <summary>Per-file outcome of a health scan (master doc §11.3 F-020/F-025).</summary>
public enum FileHealthStatus
{
    /// <summary>Parsed cleanly (and, for prospects, blob SHA-1 matched).</summary>
    Ok,

    /// <summary>The file was not present.</summary>
    Missing,

    /// <summary>Present but failed to parse as valid JSON.</summary>
    Unparseable,

    /// <summary>Parsed, but a prospect blob's SHA-1 did not match its recorded hash.</summary>
    BlobHashMismatch,

    /// <summary>Present but could not be read (I/O or access error).</summary>
    Unreadable,
}
