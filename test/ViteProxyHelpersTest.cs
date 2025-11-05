using Microsoft.AspNetCore.Http;
using Web;

namespace Viper.test
{
    public class ViteProxyHelpersTest
    {
        private readonly string[] _vueAppNames = { "CTS", "Computing", "Students", "ClinicalScheduler" };

        [Theory]
        [InlineData("/CTS", true)]
        [InlineData("/Computing", true)]
        [InlineData("/Students/dashboard", true)]
        [InlineData("/ClinicalScheduler/schedule", true)]
        [InlineData("/vue/assets/main.js", true)]
        [InlineData("/2/vue/src/CTS/index.html", true)]
        [InlineData("/CTS/component.vue", true)]
        [InlineData("/cts.ts", true)]
        [InlineData("/computing.js", true)]
        public void ShouldProxyToVite_AllowedPaths_ReturnsTrue(string path, bool expected)
        {
            // Arrange
            var context = CreateHttpContext(path);

            // Act
            var result = ViteProxyHelpers.ShouldProxyToVite(context, _vueAppNames);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("/api/cts/students", false)]
        [InlineData("/admin", false)]
        [InlineData("/2/vue/assets/main-abc123.js", false)]
        [InlineData("/2/vue/assets/styles-def456.css", false)]
        [InlineData("/favicon.ico", false)]
        [InlineData("/robots.txt", false)]
        [InlineData("/unknown/path", false)]
        [InlineData("/malicious/../../../etc/passwd", false)]
        public void ShouldProxyToVite_DeniedPaths_ReturnsFalse(string path, bool expected)
        {
            // Arrange
            var context = CreateHttpContext(path);

            // Act
            var result = ViteProxyHelpers.ShouldProxyToVite(context, _vueAppNames);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ShouldProxyToVite_EmptyVueAppNames_ReturnsFalseForAppRoutes()
        {
            // Arrange
            var context = CreateHttpContext("/CTS");
            var emptyAppNames = Array.Empty<string>();

            // Act
            var result = ViteProxyHelpers.ShouldProxyToVite(context, emptyAppNames);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldProxyToVite_NullPath_ReturnsFalse()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = PathString.Empty;

            // Act
            var result = ViteProxyHelpers.ShouldProxyToVite(context, _vueAppNames);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("/CTS", "", "https://localhost:5173/2/vue/src/CTS/index.html")]
        [InlineData("/Students", "", "https://localhost:5173/2/vue/src/Students/index.html")]
        [InlineData("/Computing", "", "https://localhost:5173/2/vue/src/Computing/index.html")]
        [InlineData("/CTS/dashboard", "", "https://localhost:5173/2/vue/src/CTS/index.html")]
        [InlineData("/Students/profile", "", "https://localhost:5173/2/vue/src/Students/index.html")]
        [InlineData("/cts.ts", "", "https://localhost:5173/2/vue/src/CTS/cts.ts")]
        [InlineData("/students.ts", "", "https://localhost:5173/2/vue/src/Students/students.ts")]
        [InlineData("/computing.ts", "", "https://localhost:5173/2/vue/src/Computing/computing.ts")]
        [InlineData("/CTS/cts.ts", "", "https://localhost:5173/2/vue/src/CTS/cts.ts")]
        [InlineData("/Students/students.ts", "", "https://localhost:5173/2/vue/src/Students/students.ts")]
        [InlineData("/2/vue/src/CTS/component.vue", "", "https://localhost:5173/2/vue/src/CTS/component.vue")]
        [InlineData("/vue/assets/main.js", "", "https://localhost:5173/vue/assets/main.js")]
        [InlineData("/CTS", "?debug=true", "https://localhost:5173/2/vue/src/CTS/index.html?debug=true")]
        public void BuildViteUrl_ValidPaths_ReturnsCorrectUrl(string path, string queryString, string expected)
        {
            // Arrange - clear environment variable to test with hardcoded default
            var original = Environment.GetEnvironmentVariable("VITE_SERVER_URL");
            try
            {
                Environment.SetEnvironmentVariable("VITE_SERVER_URL", null);
                var pathString = new PathString(path);
                var query = new QueryString(queryString);

                // Act
                var result = ViteProxyHelpers.BuildViteUrl(pathString, query, _vueAppNames);

                // Assert
                Assert.Equal(expected, result);
            }
            finally
            {
                Environment.SetEnvironmentVariable("VITE_SERVER_URL", original);
            }
        }

        private static HttpContext CreateHttpContext(string path)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = new PathString(path);
            return context;
        }

        [Fact]
        public void BuildViteUrl_RejectsEnvWithCredentials()
        {
            // Arrange
            var original = Environment.GetEnvironmentVariable("VITE_SERVER_URL");
            try
            {
                // Set an environment variable with userinfo (should be rejected)
                Environment.SetEnvironmentVariable("VITE_SERVER_URL", "https://user:pass@evil.example.com");
                var pathString = new PathString("/CTS");
                var query = new QueryString("");

                // Act
                var result = ViteProxyHelpers.BuildViteUrl(pathString, query, _vueAppNames);

                // Assert - should fall back to localhost:5173 (hardcoded default) and not include the evil host
                Assert.StartsWith("https://localhost:5173", result);
                Assert.DoesNotContain("evil.example.com", result);
            }
            finally
            {
                Environment.SetEnvironmentVariable("VITE_SERVER_URL", original);
            }
        }

        [Fact]
        public void CreateProxyRequest_DoesNotForwardHopByHopHeaders()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Headers["Connection"] = "keep-alive";
            context.Request.Headers["Host"] = "example.local";
            context.Request.Headers["X-Custom"] = "value";

            // Act
            var req = ViteProxyHelpers.CreateProxyRequest(context, "https://localhost:5173/cts");

            // Assert - Host and Connection should not be forwarded as headers
            Assert.False(req.Headers.Contains("Connection"));
            Assert.False(req.Headers.Contains("Host"));
            // Custom header should be forwarded
            Assert.True(req.Headers.Contains("X-Custom"));
        }
    }
}
