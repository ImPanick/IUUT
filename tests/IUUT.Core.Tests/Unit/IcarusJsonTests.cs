using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using IUUT.Core.Io;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class IcarusJsonTests
{
    private sealed class Sample
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    [Fact]
    public void Serialize_ThenDeserialize_RoundTripsValues()
    {
        var original = new Sample { Name = "Char1 & Co", Count = 42 };

        var json = IcarusJson.Serialize(original);
        var back = IcarusJson.Deserialize<Sample>(json);

        back.Name.Should().Be("Char1 & Co");
        back.Count.Should().Be(42);
    }

    [Fact]
    public void Serialize_WithRelaxedEncoder_DoesNotEscapeAmpersand()
    {
        var json = IcarusJson.Serialize(new Sample { Name = "A & B" });

        json.Should().Contain("A & B");
        json.Should().NotContain("\\u0026");
    }

    [Fact]
    public void Deserialize_PreservesUnknownMembers_ViaExtensionData()
    {
        const string json = """
            { "Name": "x", "Count": 1, "FutureField": { "nested": true } }
            """;

        var model = IcarusJson.Deserialize<Sample>(json);

        model.Extra.Should().ContainKey("FutureField");

        // Round-trip must reproduce the unknown member (CONSTITUTION VI).
        var reserialized = IcarusJson.Serialize(model);
        reserialized.Should().Contain("FutureField");
        reserialized.Should().Contain("nested");
    }

    [Fact]
    public void Deserialize_NullLiteral_Throws()
    {
        var act = () => IcarusJson.Deserialize<Sample>("null");

        act.Should().Throw<JsonException>();
    }
}
