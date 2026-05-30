namespace IUUT.Core.Abstractions;

/// <summary>
/// Default <see cref="IGuidProvider"/> backed by <see cref="Guid.NewGuid"/>.
/// </summary>
public sealed class SystemGuidProvider : IGuidProvider
{
    /// <inheritdoc />
    public Guid NewGuid() => Guid.NewGuid();
}
