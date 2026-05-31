using FluentAssertions;
using IUUT.Core.Services;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class SteamLoginUsersParserTests
{
    private const string SampleVdf = """
        "users"
        {
        	"00000000000000001"
        	{
        		"AccountName"		"acct1"
        		"PersonaName"		"TestUserOne"
        		"RememberPassword"	"1"
        		"MostRecent"		"1"
        	}
        	"00000000000000002"
        	{
        		"AccountName"		"acct2"
        		"PersonaName"		"TestUserTwo"
        	}
        }
        """;

    [Fact]
    public void Parse_ExtractsPersonaNamesBySteamId()
    {
        var map = SteamLoginUsersParser.Parse(SampleVdf);

        map.Should().HaveCount(2);
        map["00000000000000001"].Should().Be("TestUserOne");
        map["00000000000000002"].Should().Be("TestUserTwo");
    }

    [Fact]
    public void Parse_NoUsersBlock_ReturnsEmpty()
    {
        SteamLoginUsersParser.Parse("\"other\" { \"x\" \"1\" }").Should().BeEmpty();
    }

    [Fact]
    public void Parse_AccountWithoutPersonaName_IsSkipped()
    {
        var map = SteamLoginUsersParser.Parse("\"users\" { \"00000000000000003\" { \"AccountName\" \"a\" } }");

        map.Should().BeEmpty();
    }
}
