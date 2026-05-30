namespace IUUT.Core.Abstractions;

/// <summary>
/// Default <see cref="IClock"/> backed by the real system clock. This is the one
/// sanctioned place that reads <see cref="DateTimeOffset.UtcNow"/> directly.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
