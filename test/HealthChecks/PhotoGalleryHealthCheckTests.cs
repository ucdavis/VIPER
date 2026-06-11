using Microsoft.Extensions.Diagnostics.HealthChecks;
using Viper.Classes.HealthChecks;

namespace Viper.test.HealthChecks
{
    public sealed class PhotoGalleryHealthCheckTests : IDisposable
    {
        private readonly string _tempDir;

        public PhotoGalleryHealthCheckTests()
        {
            _tempDir = Path.Join(Path.GetTempPath(), "photo-gallery-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private void WriteFile(string name) => File.WriteAllBytes(Path.Join(_tempDir, name), Array.Empty<byte>());

        private static HealthCheckContext CreateContext(PhotoGalleryHealthCheck sut) => new()
        {
            Registration = new HealthCheckRegistration("photo-gallery", sut, null, null)
        };

        private static async Task<HealthCheckResult> RunAsync(PhotoGalleryHealthCheck sut) =>
            await sut.CheckHealthAsync(CreateContext(sut), TestContext.Current.CancellationToken);

        [Fact]
        public async Task CheckHealthAsync_HealthyWhenDirectoryHasPhotos()
        {
            WriteFile("alice.jpg");
            WriteFile("bob.jpg");

            var result = await RunAsync(new PhotoGalleryHealthCheck(_tempDir));

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains("Photo gallery OK", result.Description);
            Assert.Equal(2, result.Data["photo_count"]);
            Assert.Equal(_tempDir, result.Data["path"]);
        }

        [Fact]
        public async Task CheckHealthAsync_UnhealthyWhenDirectoryEmpty()
        {
            var result = await RunAsync(new PhotoGalleryHealthCheck(_tempDir));

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("too few photos", result.Description);
            Assert.Equal(0, result.Data["photo_count"]);
        }

        [Fact]
        public async Task CheckHealthAsync_UnhealthyWhenOnlyOnePhoto()
        {
            // A lone photo (e.g. just the nopic.jpg placeholder) is not a healthy gallery.
            WriteFile("lonely.jpg");

            var result = await RunAsync(new PhotoGalleryHealthCheck(_tempDir));

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("too few photos", result.Description);
            Assert.Equal(1, result.Data["photo_count"]);
        }

        [Fact]
        public async Task CheckHealthAsync_OnlyCountsExactJpgExtension()
        {
            WriteFile("alice.jpg");
            WriteFile("bob.jpg");
            // 3-char "*.jpg" pattern would otherwise match these on Windows.
            WriteFile("notaphoto.jpginfo");
            WriteFile("readme.txt");

            var result = await RunAsync(new PhotoGalleryHealthCheck(_tempDir));

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Equal(2, result.Data["photo_count"]);
        }

        [Fact]
        public async Task CheckHealthAsync_DegradedWhenDirectoryUnreachable()
        {
            // Unreachable share is Degraded, not Unhealthy: the app still works,
            // falling back to the default image.
            var missing = Path.Join(_tempDir, "does-not-exist");

            var result = await RunAsync(new PhotoGalleryHealthCheck(missing));

            Assert.Equal(HealthStatus.Degraded, result.Status);
            Assert.Contains("not reachable", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_HealthyWhenMissingAndDirectoryMissing()
        {
            var missing = Path.Join(_tempDir, "does-not-exist");

            var result = await RunAsync(new PhotoGalleryHealthCheck(missing, healthyWhenMissing: true));

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains("skipped", result.Description);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CheckHealthAsync_UnhealthyWhenPathNotConfigured(string? path)
        {
            var result = await RunAsync(new PhotoGalleryHealthCheck(path));

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("not configured", result.Description);
        }
    }
}
