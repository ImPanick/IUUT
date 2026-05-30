namespace IUUT.Core.Abstractions;

/// <summary>
/// Abstraction over GUID creation so code that generates identifiers (e.g. fresh
/// <c>DatabaseGUID</c> values for new stash items) is deterministic in tests.
/// Inject this instead of calling <see cref="System.Guid.NewGuid"/> directly
/// (CODE_STYLE §9).
/// </summary>
public interface IGuidProvider
{
    /// <summary>Creates a new GUID.</summary>
    Guid NewGuid();
}
