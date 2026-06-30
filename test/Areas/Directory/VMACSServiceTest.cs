using Viper.Areas.Directory.Services;

namespace Viper.test.Areas.Directory
{
    public class VMACSServiceTest
    {
        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("jdoe", "jdoe")]
        [InlineData("john doe", "john%20doe")]
        [InlineData("a+b", "a%2Bb")]
        [InlineData("a&b", "a%26b")]
        [InlineData("a=b", "a%3Db")]
        public void BuildSearchPath_UrlEncodesLoginId(string? loginID, string expectedFind)
        {
            string path = VMACSService.BuildSearchPath(loginID, "TESTAUTH");

            Assert.Equal(
                $"/trust/query.xml?dbfile=3&index=CampusLoginId&find={expectedFind}&format=CHRIS4&AUTH=TESTAUTH",
                path);
        }

        [Fact]
        public void BuildSearchPath_IncludesAuthToken()
        {
            string path = VMACSService.BuildSearchPath("jdoe", "SECRET123");

            Assert.Contains("&AUTH=SECRET123", path);
        }
    }
}
