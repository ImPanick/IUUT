using IUUT.Core.Abstractions;

namespace IUUT.Core.Tests.TestDoubles;

/// <summary>
/// Deterministic <see cref="IClock"/> for tests. <see cref="UtcNow"/> returns a
/// fixed instant unless advanced via <see cref="Advance"/>.
/// </summary>
public sealed class FixedClock : IClock
{
    /// <summary>Creates a clock pinned to <paramref name="utcNow"/>.</summary>
    public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    /// <summary>A convenient default instant (2020-01-01T00:00:00Z).</summary>
    public static FixedClock Default => new(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

    /// <inheritdoc />
    public DateTimeOffset UtcNow { get; private set; }

    /// <summary>Moves the clock forward by <paramref name="delta"/>.</summary>
    public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
}
