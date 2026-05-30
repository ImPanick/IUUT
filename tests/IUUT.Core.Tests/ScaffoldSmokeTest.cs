using FluentAssertions;
using Xunit;

namespace IUUT.Core.Tests;

/// <summary>
/// Smoke test verifying the test harness wires up.
/// </summary>
/// <remarks>
/// Authority: .agent/TESTING_CONTRACT.md §2. This is a scaffold-time placeholder
/// so the test runner has something to discover before real tests land. Delete
/// when the first real test arrives.
/// </remarks>
public class ScaffoldSmokeTest
{
    [Fact]
    public void Scaffold_TestHarness_IsAlive()
    {
        var harnessReady = true;
        harnessReady.Should().BeTrue("the xUnit + FluentAssertions harness should be wired before any real test lands");
    }
}
