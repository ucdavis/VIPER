using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Viper.Areas.Scheduler.Controllers;
using Viper.Areas.Scheduler.Models.DTOs.Responses;
using Viper.Areas.Scheduler.Services;
using Viper.Models.AAUD;

namespace Viper.test.Scheduler
{
    public sealed class JobsControllerTests
    {
        private readonly ISchedulerJobsService _service;
        private readonly IUserHelper _userHelper;
        private readonly ILogger<JobsController> _logger;
        private readonly JobsController _sut;

        public JobsControllerTests()
        {
            _service = Substitute.For<ISchedulerJobsService>();
            _userHelper = Substitute.For<IUserHelper>();
            _logger = Substitute.For<ILogger<JobsController>>();
            _sut = new JobsController(_service, _userHelper, _logger);
        }

        // ──────────── ListJobs ────────────

        [Fact]
        public async Task ListJobs_ReturnsOkWithServiceResult()
        {
            var dtos = new List<SchedulerJobDto> { new() { Id = "raps:role-refresh" } };
            _service.ListJobsAsync(Arg.Any<CancellationToken>()).Returns(dtos);

            var result = await _sut.ListJobs(CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(dtos, ok.Value);
        }

        // ──────────── GetJob ────────────

        [Fact]
        public async Task GetJob_ReturnsOkWhenServiceFindsJob()
        {
            var dto = new SchedulerJobDto { Id = "raps:role-refresh" };
            _service.GetJobAsync("raps:role-refresh", Arg.Any<CancellationToken>()).Returns(dto);

            var result = await _sut.GetJob("raps:role-refresh", CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(dto, ok.Value);
        }

        [Fact]
        public async Task GetJob_ReturnsNotFoundWhenServiceReturnsNull()
        {
            _service.GetJobAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((SchedulerJobDto?)null);

            var result = await _sut.GetJob("missing", CancellationToken.None);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ──────────── PauseJob ────────────

        [Fact]
        public async Task PauseJob_ReturnsOkWhenServiceCompletes()
        {
            _userHelper.GetCurrentUser().Returns(new AaudUser { LoginId = "alice" });
            var dto = new PauseResumeResultDto { Id = "raps:role-refresh", IsPaused = true };
            _service.PauseJobAsync("raps:role-refresh", "alice", Arg.Any<byte[]?>(), Arg.Any<CancellationToken>())
                .Returns(dto);

            var result = await _sut.PauseJob("raps:role-refresh", new JobsController.PauseRequest(), CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(dto, ok.Value);
        }

        [Fact]
        public async Task PauseJob_Returns202WhenDeregistrationPending()
        {
            _userHelper.GetCurrentUser().Returns(new AaudUser { LoginId = "alice" });
            var dto = new PauseResumeResultDto { Id = "raps:role-refresh", IsPaused = true, DeregistrationPending = true };
            _service.PauseJobAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>(), Arg.Any<CancellationToken>())
                .Returns(dto);

            var result = await _sut.PauseJob("raps:role-refresh", null, CancellationToken.None);

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status202Accepted, status.StatusCode);
            Assert.Same(dto, status.Value);
        }

        [Fact]
        public async Task PauseJob_Returns403OnSystemJobProtection()
        {
            _userHelper.GetCurrentUser().Returns(new AaudUser { LoginId = "alice" });
            _service.PauseJobAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>(), Arg.Any<CancellationToken>())
                .ThrowsAsyncForAnyArgs(new SchedulerSystemJobProtectedException("__scheduler:reconcile"));

            var result = await _sut.PauseJob("__scheduler:reconcile", null, CancellationToken.None);

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);
        }

        [Fact]
        public async Task PauseJob_Returns404WhenJobMissing()
        {
            _userHelper.GetCurrentUser().Returns(new AaudUser { LoginId = "alice" });
            _service.PauseJobAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>(), Arg.Any<CancellationToken>())
                .ThrowsAsyncForAnyArgs(new SchedulerJobNotFoundException("nope"));

            var result = await _sut.PauseJob("nope", null, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PauseJob_Returns409OnConcurrencyConflict()
        {
            _userHelper.GetCurrentUser().Returns(new AaudUser { LoginId = "alice" });
            _service.PauseJobAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>(), Arg.Any<CancellationToken>())
                .ThrowsAsyncForAnyArgs(new SchedulerConcurrencyException("raps:role-refresh"));

            var result = await _sut.PauseJob("raps:role-refresh", null, CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task PauseJob_Returns400OnInvalidBase64RowVersion()
        {
            _userHelper.GetCurrentUser().Returns(new AaudUser { LoginId = "alice" });

            var result = await _sut.PauseJob(
                "raps:role-refresh",
                new JobsController.PauseRequest { RowVersion = "not-base-64!!!" },
                CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
            await _service.DidNotReceiveWithAnyArgs().PauseJobAsync(default!, default!, default, default);
        }

        [Fact]
        public async Task PauseJob_FallsBackToSchedulerActorWhenNoUser()
        {
            _userHelper.GetCurrentUser().Returns((AaudUser?)null);
            _service.PauseJobAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>(), Arg.Any<CancellationToken>())
                .Returns(new PauseResumeResultDto { Id = "raps:role-refresh", IsPaused = true });

            await _sut.PauseJob("raps:role-refresh", null, CancellationToken.None);

            await _service.Received(1).PauseJobAsync(
                "raps:role-refresh",
                ISchedulerJobsService.SchedulerActor,
                Arg.Any<byte[]?>(),
                Arg.Any<CancellationToken>());
        }

        // ──────────── ResumeJob ────────────

        [Fact]
        public async Task ResumeJob_ReturnsOkWhenServiceCompletes()
        {
            var dto = new PauseResumeResultDto { Id = "raps:role-refresh", IsPaused = false };
            _service.ResumeJobAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(dto);

            var result = await _sut.ResumeJob(
                "raps:role-refresh",
                new JobsController.ResumeRequest { RowVersion = Convert.ToBase64String([1, 2, 3, 4]) },
                CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(dto, ok.Value);
        }

        [Fact]
        public async Task ResumeJob_Returns400WhenRowVersionMissing()
        {
            var result = await _sut.ResumeJob("raps:role-refresh", new JobsController.ResumeRequest(), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
            await _service.DidNotReceiveWithAnyArgs().ResumeJobAsync(default!, default!, default);
        }

        [Fact]
        public async Task ResumeJob_Returns400WhenBodyMissing()
        {
            var result = await _sut.ResumeJob("raps:role-refresh", null, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ResumeJob_Returns400OnInvalidBase64()
        {
            var result = await _sut.ResumeJob(
                "raps:role-refresh",
                new JobsController.ResumeRequest { RowVersion = "***bad***" },
                CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ResumeJob_Returns403OnSystemJobProtection()
        {
            _service.ResumeJobAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                .ThrowsAsyncForAnyArgs(new SchedulerSystemJobProtectedException("__scheduler:reconcile"));

            var result = await _sut.ResumeJob(
                "__scheduler:reconcile",
                new JobsController.ResumeRequest { RowVersion = Convert.ToBase64String([1]) },
                CancellationToken.None);

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);
        }

        [Fact]
        public async Task ResumeJob_Returns404WithMarkerNotFoundCodeWhenMissing()
        {
            _service.ResumeJobAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                .ThrowsAsyncForAnyArgs(new SchedulerJobNotFoundException("nope"));

            var result = await _sut.ResumeJob(
                "nope",
                new JobsController.ResumeRequest { RowVersion = Convert.ToBase64String([1]) },
                CancellationToken.None);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(
                "marker_not_found",
                notFound.Value?.GetType().GetProperty("error")?.GetValue(notFound.Value));
        }

        [Fact]
        public async Task ResumeJob_Returns409OnConcurrencyConflict()
        {
            _service.ResumeJobAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                .ThrowsAsyncForAnyArgs(new SchedulerConcurrencyException("raps:role-refresh"));

            var result = await _sut.ResumeJob(
                "raps:role-refresh",
                new JobsController.ResumeRequest { RowVersion = Convert.ToBase64String([1]) },
                CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(result.Result);
        }
    }
}
