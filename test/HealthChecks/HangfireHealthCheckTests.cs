using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Viper.Classes.HealthChecks;

namespace Viper.test.HealthChecks
{
    public class HangfireHealthCheckTests
    {
        private static HealthCheckContext CreateContext(HangfireHealthCheck sut)
        {
            return new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("hangfire", sut, null, null)
            };
        }

        private static (Hangfire.JobStorage storage, Hangfire.Storage.IMonitoringApi monitoring) CreateStorage()
        {
            var monitoring = Substitute.For<Hangfire.Storage.IMonitoringApi>();
            var storage = Substitute.For<Hangfire.JobStorage>();
            storage.GetMonitoringApi().Returns(monitoring);
            return (storage, monitoring);
        }

        private static Hangfire.Storage.Monitoring.StatisticsDto SampleStats(long servers = 1) => new()
        {
            Servers = servers,
            Enqueued = 2,
            Scheduled = 3,
            Processing = 4,
            Failed = 5,
            Recurring = 6
        };

        [Fact]
        public async Task CheckHealthAsync_HealthyWhenServersHaveRecentHeartbeats()
        {
            var (storage, monitoring) = CreateStorage();
            monitoring.GetStatistics().Returns(SampleStats());
            monitoring.Servers().Returns(new List<Hangfire.Storage.Monitoring.ServerDto>
            {
                new() { Name = "srv-1", Heartbeat = DateTime.UtcNow }
            });

            var sut = new HangfireHealthCheck(storage);
            var result = await sut.CheckHealthAsync(CreateContext(sut));

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains("Hangfire OK", result.Description);
            Assert.Equal(1L, result.Data["servers"]);
        }

        [Fact]
        public async Task CheckHealthAsync_DegradedWhenNoServersRegistered()
        {
            var (storage, monitoring) = CreateStorage();
            monitoring.GetStatistics().Returns(SampleStats(0));
            monitoring.Servers().Returns(new List<Hangfire.Storage.Monitoring.ServerDto>());

            var sut = new HangfireHealthCheck(storage);
            var result = await sut.CheckHealthAsync(CreateContext(sut));

            Assert.Equal(HealthStatus.Degraded, result.Status);
            Assert.Contains("no servers registered", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_UnhealthyWhenAllHeartbeatsStale()
        {
            var (storage, monitoring) = CreateStorage();
            monitoring.GetStatistics().Returns(SampleStats());
            monitoring.Servers().Returns(new List<Hangfire.Storage.Monitoring.ServerDto>
            {
                new() { Name = "srv-stale", Heartbeat = DateTime.UtcNow.AddMinutes(-10) }
            });

            var sut = new HangfireHealthCheck(storage);
            var result = await sut.CheckHealthAsync(CreateContext(sut));

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("stale", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_UnhealthyWhenServerHasNullHeartbeat()
        {
            var (storage, monitoring) = CreateStorage();
            monitoring.GetStatistics().Returns(SampleStats());
            monitoring.Servers().Returns(new List<Hangfire.Storage.Monitoring.ServerDto>
            {
                new() { Name = "srv-never", Heartbeat = null }
            });

            var sut = new HangfireHealthCheck(storage);
            var result = await sut.CheckHealthAsync(CreateContext(sut));

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("never", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_HealthyWhenAtLeastOneHeartbeatIsRecent()
        {
            var (storage, monitoring) = CreateStorage();
            monitoring.GetStatistics().Returns(SampleStats(2));
            monitoring.Servers().Returns(new List<Hangfire.Storage.Monitoring.ServerDto>
            {
                new() { Name = "srv-stale", Heartbeat = DateTime.UtcNow.AddMinutes(-10) },
                new() { Name = "srv-fresh", Heartbeat = DateTime.UtcNow }
            });

            var sut = new HangfireHealthCheck(storage);
            var result = await sut.CheckHealthAsync(CreateContext(sut));

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains("Hangfire OK", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_UnhealthyWhenStorageThrows()
        {
            var storage = Substitute.For<Hangfire.JobStorage>();
            var boom = new InvalidOperationException("boom");
            storage.GetMonitoringApi().Returns(_ => throw boom);

            var sut = new HangfireHealthCheck(storage);
            var result = await sut.CheckHealthAsync(CreateContext(sut));

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("unreachable", result.Description);
            Assert.Same(boom, result.Exception);
        }

        [Fact]
        public async Task CheckHealthAsync_DataDictionaryContainsAllStatsKeys()
        {
            var (storage, monitoring) = CreateStorage();
            monitoring.GetStatistics().Returns(new Hangfire.Storage.Monitoring.StatisticsDto
            {
                Servers = 1,
                Enqueued = 7,
                Scheduled = 8,
                Processing = 9,
                Failed = 10,
                Recurring = 11
            });
            monitoring.Servers().Returns(new List<Hangfire.Storage.Monitoring.ServerDto>
            {
                new() { Name = "srv-1", Heartbeat = DateTime.UtcNow }
            });

            var sut = new HangfireHealthCheck(storage);
            var result = await sut.CheckHealthAsync(CreateContext(sut));

            Assert.Contains("servers", result.Data.Keys);
            Assert.Contains("enqueued", result.Data.Keys);
            Assert.Contains("scheduled", result.Data.Keys);
            Assert.Contains("processing", result.Data.Keys);
            Assert.Contains("failed", result.Data.Keys);
            Assert.Contains("recurring", result.Data.Keys);
            Assert.Equal(1L, result.Data["servers"]);
            Assert.Equal(7L, result.Data["enqueued"]);
            Assert.Equal(11L, result.Data["recurring"]);
        }
    }
}
