using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;
using Viper.Classes.Scheduler;

namespace Viper.test.Scheduler
{
    public class HangfireJobLoggingFilterTests
    {
        // Concrete method target for Hangfire.Common.Job — needs a real MethodInfo.
        private static class FakeJob
        {
            public static void Run(string s, int i)
            {
                _ = s;
                _ = i;
            }
        }

        private static Hangfire.Server.PerformContext BuildPerformContext(
            string? recurringJobId = null,
            string jobId = "123")
        {
            var method = typeof(FakeJob).GetMethod(nameof(FakeJob.Run))!;
            var job = new Hangfire.Common.Job(typeof(FakeJob), method, new object[] { "arg1", 42 });
            var backgroundJob = new Hangfire.BackgroundJob(jobId, job, DateTime.UtcNow);

            var storage = Substitute.For<Hangfire.JobStorage>();
            var connection = Substitute.For<Hangfire.Storage.IStorageConnection>();
            // Hangfire JSON-deserializes parameter values via SerializationHelper, so the mock
            // must return a JSON-encoded string (or null), not a raw value.
            var encodedParam = recurringJobId is null
                ? null
                : Hangfire.Common.SerializationHelper.Serialize(recurringJobId);
            connection.GetJobParameter(jobId, "RecurringJobId").Returns(encodedParam);

            var cancel = Substitute.For<Hangfire.IJobCancellationToken>();
            return new Hangfire.Server.PerformContext(storage, connection, backgroundJob, cancel);
        }

        private static IDisposable CaptureScope(ILogger<HangfireJobLoggingFilter> logger, out List<Dictionary<string, object>> captured)
        {
            var list = new List<Dictionary<string, object>>();
            captured = list;
            var scope = Substitute.For<IDisposable>();
            logger.BeginScope(Arg.Do<Dictionary<string, object>>(d => list.Add(d))).Returns(scope);
            return scope;
        }

        // ILogger.Log<TState> is generic; the TState passed by extension methods is an internal
        // struct (FormattedLogValues) which makes Arg.Any<object>() unreliable. Inspecting the
        // recorded calls directly avoids the generic-arg matching problem.
        private static IReadOnlyList<ICall> GetLogCalls(ILogger logger, LogLevel level)
        {
            return logger.ReceivedCalls()
                .Where(c => c.GetMethodInfo().Name == nameof(ILogger.Log)
                            && c.GetArguments().Length >= 1
                            && c.GetArguments()[0] is LogLevel l
                            && l == level)
                .ToList();
        }

        private static string GetMessage(ICall call)
        {
            // Args layout: [LogLevel, EventId, TState, Exception, Func<TState, Exception, string>]
            var args = call.GetArguments();
            return args[2]?.ToString() ?? string.Empty;
        }

        private static Exception? GetException(ICall call)
        {
            return call.GetArguments()[3] as Exception;
        }

        [Fact]
        public void OnPerforming_LogsStartMessageWithSanitizedJobInfo()
        {
            var logger = Substitute.For<ILogger<HangfireJobLoggingFilter>>();
            CaptureScope(logger, out _);
            var sut = new HangfireJobLoggingFilter(logger);
            var perf = BuildPerformContext();
            var performing = new Hangfire.Server.PerformingContext(perf);

            sut.OnPerforming(performing);

            var infoCalls = GetLogCalls(logger, LogLevel.Information);
            Assert.Single(infoCalls);
            Assert.Contains("Hangfire job starting", GetMessage(infoCalls[0]));
        }

        [Fact]
        public void OnPerforming_BeginsScopeWithJobMetadata()
        {
            var logger = Substitute.For<ILogger<HangfireJobLoggingFilter>>();
            CaptureScope(logger, out var captured);
            var sut = new HangfireJobLoggingFilter(logger);
            var perf = BuildPerformContext(recurringJobId: "daily-cleanup");
            var performing = new Hangfire.Server.PerformingContext(perf);

            sut.OnPerforming(performing);

            logger.Received(1).BeginScope(Arg.Any<Dictionary<string, object>>());
            Assert.Single(captured);
            var dict = captured[0];
            Assert.Contains("jobId", dict.Keys);
            Assert.Contains("recurringJobId", dict.Keys);
            Assert.Contains("triggerSource", dict.Keys);
            Assert.Equal("Scheduled", dict["triggerSource"]);
        }

        [Fact]
        public void OnPerforming_TriggerSourceIsManualWhenNoRecurringJobId()
        {
            var logger = Substitute.For<ILogger<HangfireJobLoggingFilter>>();
            CaptureScope(logger, out var captured);
            var sut = new HangfireJobLoggingFilter(logger);
            var perf = BuildPerformContext(recurringJobId: null);
            var performing = new Hangfire.Server.PerformingContext(perf);

            sut.OnPerforming(performing);

            Assert.Single(captured);
            Assert.Equal("Manual", captured[0]["triggerSource"]);
        }

        [Fact]
        public void OnPerformed_LogsCompletionWhenNoException()
        {
            var logger = Substitute.For<ILogger<HangfireJobLoggingFilter>>();
            CaptureScope(logger, out _);
            var sut = new HangfireJobLoggingFilter(logger);
            var perf = BuildPerformContext();
            var performed = new Hangfire.Server.PerformedContext(perf, result: null, canceled: false, exception: null);

            sut.OnPerformed(performed);

            var infoCalls = GetLogCalls(logger, LogLevel.Information);
            Assert.Single(infoCalls);
            Assert.Contains("Hangfire job completed", GetMessage(infoCalls[0]));
        }

        [Fact]
        public void OnPerformed_LogsErrorWhenExceptionPresent()
        {
            var logger = Substitute.For<ILogger<HangfireJobLoggingFilter>>();
            CaptureScope(logger, out _);
            var sut = new HangfireJobLoggingFilter(logger);
            var perf = BuildPerformContext();
            var failure = new InvalidOperationException("kaboom");
            var performed = new Hangfire.Server.PerformedContext(perf, result: null, canceled: false, exception: failure);

            sut.OnPerformed(performed);

            var errorCalls = GetLogCalls(logger, LogLevel.Error);
            Assert.Single(errorCalls);
            Assert.Same(failure, GetException(errorCalls[0]));
        }

        [Fact]
        public void OnPerformed_DisposesScopeStashedByOnPerforming()
        {
            var logger = Substitute.For<ILogger<HangfireJobLoggingFilter>>();
            var scope = CaptureScope(logger, out _);
            var sut = new HangfireJobLoggingFilter(logger);
            var perf = BuildPerformContext();
            var performing = new Hangfire.Server.PerformingContext(perf);
            var performed = new Hangfire.Server.PerformedContext(perf, result: null, canceled: false, exception: null);

            sut.OnPerforming(performing);
            sut.OnPerformed(performed);

            scope.Received(1).Dispose();
        }

        [Fact]
        public void OnPerformed_DoesNotThrowWhenScopeMissing()
        {
            var logger = Substitute.For<ILogger<HangfireJobLoggingFilter>>();
            CaptureScope(logger, out _);
            var sut = new HangfireJobLoggingFilter(logger);
            var perf = BuildPerformContext();
            var performed = new Hangfire.Server.PerformedContext(perf, result: null, canceled: false, exception: null);

            // No prior OnPerforming, so context.Items has no scope key.
            var ex = Record.Exception(() => sut.OnPerformed(performed));
            Assert.Null(ex);
        }
    }
}
