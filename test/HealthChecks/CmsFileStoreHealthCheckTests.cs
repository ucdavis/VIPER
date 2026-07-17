using Microsoft.Extensions.Diagnostics.HealthChecks;
using Viper.Classes.HealthChecks;

namespace Viper.test.HealthChecks
{
    public sealed class CmsFileStoreHealthCheckTests : IDisposable
    {
        private readonly string _root;

        public CmsFileStoreHealthCheckTests()
        {
            _root = Path.Join(Path.GetTempPath(), "cms-store-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }

        private void AddFolder(string name) => Directory.CreateDirectory(Path.Join(_root, name));

        private static async Task<HealthCheckResult> RunAsync(CmsFileStoreHealthCheck sut) =>
            await sut.CheckHealthAsync(
                new HealthCheckContext { Registration = new HealthCheckRegistration("disk-space-cms-writable", sut, null, null) },
                TestContext.Current.CancellationToken);

        [Fact]
        public async Task Healthy_WhenRootAndTopLevelFoldersWritable()
        {
            AddFolder("Apps");
            AddFolder("Development");

            var result = await RunAsync(new CmsFileStoreHealthCheck(_root));

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains("writable", result.Description);
            // The root plus the two top-level folders are all probed.
            Assert.Equal(3, result.Data["folders_checked"]);
        }

        [Fact]
        public async Task Healthy_WhenRootHasNoSubfolders_ProbesRootOnly()
        {
            var result = await RunAsync(new CmsFileStoreHealthCheck(_root));

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Equal(1, result.Data["folders_checked"]);
        }

        [Fact]
        public async Task Unhealthy_WhenRootMissing()
        {
            var missing = Path.Join(_root, "does-not-exist");

            var result = await RunAsync(new CmsFileStoreHealthCheck(missing));

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("does not exist", result.Description);
        }

        [Fact]
        public async Task Healthy_WhenRootMissingButHealthyWhenMissing()
        {
            var missing = Path.Join(_root, "does-not-exist");

            var result = await RunAsync(new CmsFileStoreHealthCheck(missing, healthyWhenMissing: true));

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains("skipped", result.Description);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Unhealthy_WhenRootNotConfigured(string? path)
        {
            var result = await RunAsync(new CmsFileStoreHealthCheck(path));

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("not configured", result.Description);
        }
    }
}
