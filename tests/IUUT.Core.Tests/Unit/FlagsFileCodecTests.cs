using FluentAssertions;
using IUUT.Core.Editing;
using IUUT.Core.Io;
using IUUT.Core.Models;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class FlagsFileCodecTests
{
    private const string SteamId = "76561198000000000"; // 17 chars

    [Fact]
    public void WriteThenRead_RoundTripsSteamIdAndFlags()
    {
        var model = new FlagsFileModel { SteamId = SteamId, Flags = [1u, 2u, 4294967295u] };

        var read = FlagsFileCodec.Read(FlagsFileCodec.Write(model));

        read.SteamId.Should().Be(SteamId);
        read.Flags.Should().Equal(1u, 2u, 4294967295u);
    }

    [Fact]
    public void Write_ProducesTheDocumented82ByteLayout_ForSeventeenCharIdAnd14Flags()
    {
        var model = new FlagsFileModel
        {
            SteamId = SteamId,
            Flags = [.. Enumerable.Range(0, 14).Select(i => (uint)i)],
        };

        var bytes = FlagsFileCodec.Write(model);

        // 4 (len prefix) + 17 (SteamID) + 1 (NUL) + 4 (count) + 14*4 (flags) = 82
        bytes.Should().HaveCount(82);
        BitConverter.ToUInt32(bytes, 0).Should().Be(18u, "the FString length prefix includes the trailing NUL");
        bytes[21].Should().Be(0, "the SteamID is NUL-terminated at offset 21");
    }

    [Fact]
    public void Read_TooShortBuffer_Throws()
    {
        var act = () => FlagsFileCodec.Read([0x12, 0x00]);
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void EditService_AddAndRemoveFlags_ReportPresence()
    {
        var service = new FlagsEditService();
        var model = new FlagsFileModel { SteamId = SteamId };

        service.AddFlag(model, 42u).Should().BeTrue();
        service.AddFlag(model, 42u).Should().BeFalse();
        service.RemoveFlag(model, 42u).Should().BeTrue();
        service.RemoveFlag(model, 42u).Should().BeFalse();
        model.Flags.Should().BeEmpty();
    }
}
