using System.Text.Json;
using FluentAssertions;
using IUUT.Core.Io;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class NestedStringifiedJsonTests
{
    private sealed class Box
    {
        public int N { get; set; }
        public string S { get; set; } = "";
    }

    [Fact]
    public void RoundTrip_WrapsAndUnwrapsItems()
    {
        var items = new[] { new Box { N = 1, S = "a" }, new Box { N = 2, S = "b" } };

        var json = NestedStringifiedJson.Serialize("Container.json", items);
        var back = NestedStringifiedJson.Parse<Box>(json, "Container.json");

        back.Should().HaveCount(2);
        back[0].N.Should().Be(1);
        back[1].S.Should().Be("b");
    }

    [Fact]
    public void Serialize_ElementsAreStringifiedJson_NotNestedObjects()
    {
        var json = NestedStringifiedJson.Serialize("Container.json", new[] { new Box { N = 7, S = "x" } });

        using var doc = JsonDocument.Parse(json);
        var array = doc.RootElement.GetProperty("Container.json");
        array.ValueKind.Should().Be(JsonValueKind.Array);
        array[0].ValueKind.Should().Be(JsonValueKind.String, "each element must be a stringified object, not a nested object");

        // The string content is itself parseable JSON.
        using var inner = JsonDocument.Parse(array[0].GetString()!);
        inner.RootElement.GetProperty("N").GetInt32().Should().Be(7);
    }

    [Fact]
    public void Parse_KeyMissing_Throws()
    {
        var act = () => NestedStringifiedJson.Parse<Box>("""{ "Other.json": [] }""", "Container.json");

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Parse_ValueNotArray_Throws()
    {
        var act = () => NestedStringifiedJson.Parse<Box>("""{ "Container.json": 5 }""", "Container.json");

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Parse_ElementNotString_Throws()
    {
        var act = () => NestedStringifiedJson.Parse<Box>("""{ "Container.json": [ { "N": 1 } ] }""", "Container.json");

        act.Should().Throw<JsonException>();
    }
}
