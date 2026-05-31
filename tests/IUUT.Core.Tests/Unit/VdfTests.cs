using FluentAssertions;
using IUUT.Core.Io;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class VdfTests
{
    [Fact]
    public void Parse_NestedObjectsAndLeaves()
    {
        const string vdf = "\"root\"\n{\n\t\"a\"\t\"1\"\n\t\"obj\"\n\t{\n\t\t\"b\"\t\"2\"\n\t}\n}";

        var node = Vdf.Parse(vdf);

        node.TryGetObject("root", out var root).Should().BeTrue();
        root.TryGetString("a", out var a).Should().BeTrue();
        a.Should().Be("1");
        root.TryGetObject("obj", out var obj).Should().BeTrue();
        obj.TryGetString("b", out var b).Should().BeTrue();
        b.Should().Be("2");
    }

    [Fact]
    public void Parse_KeysAreCaseInsensitive()
    {
        var node = Vdf.Parse("\"Users\" { \"PersonaName\" \"X\" }");

        node.TryGetObject("users", out var users).Should().BeTrue();
        users.TryGetString("personaname", out var v).Should().BeTrue();
        v.Should().Be("X");
    }

    [Fact]
    public void Parse_SkipsLineComments()
    {
        const string vdf = "\"root\"\n{\n// a comment\n\t\"a\"\t\"1\"\n}";

        var node = Vdf.Parse(vdf);

        node.TryGetObject("root", out var root).Should().BeTrue();
        root.TryGetString("a", out var a).Should().BeTrue();
        a.Should().Be("1");
    }

    [Fact]
    public void Parse_MissingKey_ReturnsFalse()
    {
        var node = Vdf.Parse("\"root\" { \"a\" \"1\" }");

        node.TryGetObject("root", out var root).Should().BeTrue();
        root.TryGetString("missing", out _).Should().BeFalse();
        root.TryGetObject("missing", out _).Should().BeFalse();
    }
}
