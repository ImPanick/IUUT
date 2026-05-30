namespace IUUT.Core.Abstractions;

/// <summary>
/// Abstraction over the system clock so time-dependent logic (e.g. backup-file
/// timestamps) is deterministic in tests. Inject this instead of calling
/// <see cref="System.DateTimeOffset.UtcNow"/> directly (CODE_STYLE §9).
/// </summary>
public interface IClock
{
    /// <summary>The current instant in UTC.</summary>
    DateTimeOffset UtcNow { get; }
}
