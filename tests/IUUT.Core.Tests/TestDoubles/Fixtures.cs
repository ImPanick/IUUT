namespace IUUT.Core.Tests.TestDoubles;

/// <summary>
/// Loads anonymized fixture files copied next to the test assembly (see the
/// fixture <c>None Include</c> in the test csproj). Tests read fixtures from here
/// rather than <c>%LOCALAPPDATA%</c> (TESTING_CONTRACT §5).
/// </summary>
internal static class Fixtures
{
    /// <summary>Reads the text of <c>fixtures/&lt;parts...&gt;</c> from the test output.</summary>
    public static string ReadText(params string[] relativeParts)
    {
        var parts = new string[relativeParts.Length + 2];
        parts[0] = AppContext.BaseDirectory;
        parts[1] = "fixtures";
        Array.Copy(relativeParts, 0, parts, 2, relativeParts.Length);
        return File.ReadAllText(Path.Combine(parts));
    }
}
