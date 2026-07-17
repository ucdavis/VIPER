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

        [Theory]
        [InlineData("https://vmacs-qa.example.edu")]
        [InlineData("http://vmacs.example.edu/")]
        public void IsValidBaseUrl_True_ForAbsoluteHttpUrls(string baseUrl)
        {
            Assert.True(VMACSService.IsValidBaseUrl(baseUrl));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-url")]
        [InlineData("/relative/path")]
        [InlineData("vmacs-qa.example.edu")]
        [InlineData("ftp://vmacs.example.edu")]
        [InlineData("file:///etc/passwd")]
        public void IsValidBaseUrl_False_ForMissingOrNonHttpUrls(string? baseUrl)
        {
            // A malformed or non-http(s) base URL must fail closed here rather than
            // reaching GetAsync, which would throw and abort the directory lookup.
            Assert.False(VMACSService.IsValidBaseUrl(baseUrl));
        }

        [Fact]
        public void Deserialize_ReturnsItem_WhenPayloadMatches()
        {
            var query = VMACSService.Deserialize(
                "<query><item dbfile=\"3\"><Name>Doe, John</Name></item></query>");

            Assert.NotNull(query);
            Assert.NotNull(query.item);
            Assert.Equal("Doe, John", query.item!.Name?.Single());
        }

        [Fact]
        public void Deserialize_ReturnsQueryWithNullItem_WhenNothingMatched()
        {
            // A valid <query> with no <item> is what VMACS returns for an unknown
            // login - it parses successfully but yields a null item.
            var query = VMACSService.Deserialize("<query><dbfile>3</dbfile></query>");

            Assert.NotNull(query);
            Assert.Null(query.item);
        }

        [Theory]
        [InlineData("not xml")]
        [InlineData("")]
        [InlineData("<query><item></query>")]
        public void Deserialize_Throws_OnInvalidXml(string xml)
        {
            Assert.Throws<InvalidOperationException>(() => VMACSService.Deserialize(xml));
        }

        [Fact]
        public void Deserialize_RejectsDtd_ForXxeProtection()
        {
            // DtdProcessing.Prohibit must reject any DOCTYPE before entity expansion.
            var withDoctype =
                "<!DOCTYPE query [<!ENTITY x \"y\">]><query><dbfile>3</dbfile></query>";

            Assert.Throws<InvalidOperationException>(() => VMACSService.Deserialize(withDoctype));
        }
    }
}
