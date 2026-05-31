using IUUT.Core.Services;

namespace IUUT.Core.Tests.TestDoubles;

/// <summary>In-memory <see cref="IRunningProcesses"/> for <see cref="GameProcessDetector"/> tests.</summary>
internal sealed class FakeRunningProcesses : IRunningProcesses
{
    private readonly List<string> _names;

    public FakeRunningProcesses(params string[] names) => _names = [.. names];

    public IReadOnlyList<string> GetProcessNames() => _names;
}
